using System.Text;
using System.Xml;
using System.Xml.Serialization;
using AlpineBits.GuestRequestProxy.Models;
using AlpineBits.GuestRequestProxy.Services;
using Microsoft.AspNetCore.Mvc;

namespace AlpineBits.GuestRequestProxy.Controllers;

[ApiController]
[Route("alpinebits")]
public sealed class AlpineBitsController(IAsaClient asaClient) : ControllerBase
{
    [HttpPost]
    [Consumes("application/xml", "text/xml")]
    [Produces("application/xml", "text/xml")]
    public async Task<IActionResult> Post(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(payload))
        {
            return BadRequest("Request body must contain an AlpineBits XML message.");
        }

        if (!TryDeserialize(payload, out _))
        {
            return BadRequest("Invalid OTA_HotelGuestRequestsNotifRQ payload.");
        }

        var (statusCode, responseBody, contentType) = await asaClient.ForwardGuestRequestAsync(payload, cancellationToken);

        Response.StatusCode = statusCode;
        Response.ContentType = contentType ?? "application/xml";
        return Content(responseBody);
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
