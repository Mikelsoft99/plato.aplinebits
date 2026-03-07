using System.Xml.Serialization;
using AlpineBits.GuestRequestProxy.Data.Entities;
using AlpineBits.GuestRequestProxy.Data.Repositories;
using AlpineBits.GuestRequestProxy.Models;
using AlpineBits.GuestRequestProxy.Services;
using Microsoft.AspNetCore.Mvc;

namespace AlpineBits.GuestRequestProxy.Controllers;

[ApiController]
[Route("widget")]
public sealed class WidgetController(
    ITenantRepository tenantRepository,
    IGuestRequestLogRepository logRepository,
    IWidgetRequestMapper mapper) : ControllerBase
{
    [HttpPost("request")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult<WidgetGuestRequestResponseDto>> Post(
        [FromBody] WidgetGuestRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.ArrivalDate >= request.DepartureDate)
        {
            return BadRequest(new WidgetGuestRequestResponseDto
            {
                Success = false,
                Message = "ArrivalDate must be before DepartureDate.",
                PmsStatusCode = 400
            });
        }

        var apiKey = Request.Headers["X-Api-Key"].ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Unauthorized(new WidgetGuestRequestResponseDto
            {
                Success = false,
                Message = "Missing X-Api-Key header.",
                PmsStatusCode = 401
            });
        }

        var tenant = await tenantRepository.GetByApiKeyAsync(apiKey, cancellationToken);
        if (tenant is null)
        {
            return Unauthorized(new WidgetGuestRequestResponseDto
            {
                Success = false,
                Message = "Invalid API key.",
                PmsStatusCode = 401
            });
        }

        var otaRequest = mapper.Map(request, tenant.HotelCode);
        var xml = Serialize(otaRequest);

        // Store request in database for later processing by AlpineBits endpoint
        await logRepository.AddAsync(new GuestRequestLog
        {
            TenantId = tenant.Id,
            Direction = "Inbound",
            Action = "action_OTA_HotelResNotif_GuestRequests",
            RequestJson = System.Text.Json.JsonSerializer.Serialize(request),
            RequestXml = xml,
            ResponseBody = null,
            StatusCode = 200,
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        return Ok(new WidgetGuestRequestResponseDto
        {
            Success = true,
            Message = "Request received and queued for processing.",
            RequestId = otaRequest.HotelReservations?.Items.FirstOrDefault()?.UniqueId?.Id,
            PmsStatusCode = 200
        });
    }

    private static string Serialize(OtaHotelGuestRequestsNotifRq request)
    {
        var serializer = new XmlSerializer(typeof(OtaHotelGuestRequestsNotifRq));
        using var writer = new StringWriter();
        serializer.Serialize(writer, request);
        return writer.ToString();
    }
}
