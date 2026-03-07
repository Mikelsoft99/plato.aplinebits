using System.Xml;
using System.Xml.Serialization;
using AlpineBits.GuestRequestProxy.Data.Repositories;
using AlpineBits.GuestRequestProxy.Models;
using Microsoft.AspNetCore.Mvc;

namespace AlpineBits.GuestRequestProxy.Controllers;

[ApiController]
[Route("alpinebits")]
public sealed class AlpineBitsController(
    ITenantRepository tenantRepository,
    IGuestRequestLogRepository logRepository) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [Produces("application/xml", "text/xml")]
    public async Task<IActionResult> Post(CancellationToken cancellationToken)
    {
        if (!Request.HasFormContentType)
        {
            return BadRequest("Request must be multipart/form-data with action and request fields.");
        }

        var form = await Request.ReadFormAsync(cancellationToken);
        var action = form["action"].ToString();
        var payload = form["request"].ToString();

        if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(payload))
        {
            return BadRequest("Form data fields 'action' and 'request' are required.");
        }

        var responseXml = action switch
        {
            "OTA_Ping:Handshaking" => BuildPingResponse(),
            "action_OTA_HotelResNotif_GuestRequests" => await HandleGuestRequestsPullAsync(cancellationToken),
            _ => BuildErrorResponse($"Unsupported action '{action}'.")
        };

        Response.StatusCode = 200;
        Response.ContentType = "application/xml";
        return Content(responseXml);
    }

    private static string BuildPingResponse()
    {
        return """
               <?xml version="1.0" encoding="UTF-8"?>
               <OTA_PingRS xmlns=\"http://www.opentravel.org/OTA/2003/05\" TimeStamp=\"2026-01-01T00:00:00Z\" Version=\"1.0\"> 
                 <Success />
               </OTA_PingRS>
               """;
    }

    private static string BuildGuestRequestsSuccessResponse()
    {
        var response = new OtaHotelGuestRequestsNotifRs
        {
            TimeStamp = DateTimeOffset.UtcNow.ToString("O"),
            Version = "1.0",
            Success = new Success()
        };

        var serializer = new XmlSerializer(typeof(OtaHotelGuestRequestsNotifRs));
        using var writer = new StringWriter();
        serializer.Serialize(writer, response);
        return writer.ToString();
    }

    private static string BuildErrorResponse(string message)
    {
        var response = new OtaHotelGuestRequestsNotifRs
        {
            TimeStamp = DateTimeOffset.UtcNow.ToString("O"),
            Version = "1.0",
            Errors = new Errors
            {
                Items =
                [
                    new ErrorItem
                    {
                        Type = "3",
                        Code = "400",
                        Value = message
                    }
                ]
            }
        };

        var serializer = new XmlSerializer(typeof(OtaHotelGuestRequestsNotifRs));
        using var writer = new StringWriter();
        serializer.Serialize(writer, response);
        return writer.ToString();
    }

    private async Task<string> HandleGuestRequestsPullAsync(CancellationToken cancellationToken)
    {
        // Try to identify tenant from Basic Auth header or default to first tenant
        var tenant = await GetTenantFromRequestAsync(cancellationToken);
        if (tenant is null)
        {
            return BuildErrorResponse("Unable to identify tenant. Please provide valid authentication.");
        }

        // Get pending requests from database
        var pendingLogs = await logRepository.GetPendingRequestsAsync(tenant.Id, cancellationToken);

        if (pendingLogs.Count == 0)
        {
            // No pending requests - return success response
            return BuildGuestRequestsSuccessResponse();
        }

        // Return the first pending request XML and mark it as processed
        var logToProcess = pendingLogs.First();
        
        if (!string.IsNullOrWhiteSpace(logToProcess.RequestXml))
        {
            // Mark as processed
            await logRepository.MarkAsProcessedAsync(new List<int> { logToProcess.Id }, cancellationToken);
            
            // Return the stored XML request
            return logToProcess.RequestXml;
        }

        return BuildGuestRequestsSuccessResponse();
    }

    private async Task<Data.Entities.HotelTenant?> GetTenantFromRequestAsync(CancellationToken cancellationToken)
    {
        // Try to extract tenant from Basic Auth
        var authHeader = Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
                var decodedCredentials = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
                var credentials = decodedCredentials.Split(':', 2);
                
                if (credentials.Length == 2)
                {
                    var username = credentials[0];
                    var tenant = await tenantRepository.GetByUsernameAsync(username, cancellationToken);
                    if (tenant is not null)
                    {
                        return tenant;
                    }
                }
            }
            catch
            {
                // Ignore auth parsing errors
            }
        }

        // Fallback: Get first active tenant
        return await tenantRepository.GetFirstActiveTenantAsync(cancellationToken);
    }

    private static bool TryDeserialize(string payload, out OtaHotelGuestRequestsNotifRq? model)
    {
        var serializer = new XmlSerializer(typeof(OtaHotelGuestRequestsNotifRq));
        using var stringReader = new StringReader(payload);
        using var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });

        try
        {
            model = serializer.Deserialize(xmlReader) as OtaHotelGuestRequestsNotifRq;
            return model is not null;
        }
        catch
        {
            model = null;
            return false;
        }
    }
}
