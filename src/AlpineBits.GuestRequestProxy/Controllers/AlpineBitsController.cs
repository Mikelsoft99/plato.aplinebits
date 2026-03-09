using AlpineBits.GuestRequestProxy.Data.Repositories;
using AlpineBits.GuestRequestProxy.Models;
using AlpineBits.GuestRequestProxy.Models.AlpineBits;
using Microsoft.AspNetCore.Mvc;
using System.Xml;
using System.Xml.Serialization;
using static AlpineBits.GuestRequestProxy.Controllers.AlpineBitsController;

namespace AlpineBits.GuestRequestProxy.Controllers;

[ApiController]
[Route("alpinebits")]
public sealed class AlpineBitsController(
    ITenantRepository tenantRepository,
    IGuestRequestLogRepository logRepository) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [Produces("application/xml")]
    public async Task<IActionResult> Post(CancellationToken cancellationToken)
    {
        AlpineBitsRQ alpineBitsRQ = new AlpineBitsRQ(Request);
        return await alpineBitsRQ.GenerateOutput(cancellationToken);
    }

    private static string BuildPingResponse(string payload)
    {
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(Models.AlpineBits.OTA_PingRQ));
        Models.AlpineBits.OTA_PingRQ? pingRequest;

        using (TextReader reader = new StringReader(payload))
        {
            pingRequest = (Models.AlpineBits.OTA_PingRQ)serializer.Deserialize(reader);
        }

        if (pingRequest is not null)
        {
            OTA_PingRS response = new()
            {
                TimeStamp = pingRequest.TimeStamp,
                Items = new object[] { pingRequest.EchoData },
            };
            var responseSerializer = new System.Xml.Serialization.XmlSerializer(typeof(OTA_PingRS));
            using TextWriter writer = new StringWriter();
            responseSerializer.Serialize(writer, response);
            return writer.ToString() ?? "";
        }


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



    // helper methods
    // Nicht-generisches Basis-Interface für polymorphe Speicherung
    public interface IAlpineBitsActionHandler
    {
        string Action { get; }
        Type RequestType { get; }
        Type ResponseType { get; }
        object Process(object request);
    }
    // Generisches Interface für typsichere Implementierung
    public interface IAlpineBitsActionHandler<TIn, TOut> : IAlpineBitsActionHandler
    {
        TOut Work(TIn data);

        Type IAlpineBitsActionHandler.RequestType => typeof(TIn);
        Type IAlpineBitsActionHandler.ResponseType => typeof(TOut);

        object IAlpineBitsActionHandler.Process(object request) => Work((TIn)request)!;
    }

    public class AlpineBitsTypes
    {
        public Type RequestType { get; set; }
        public Type ResponseType { get; set; }
        public AlpineBitsTypes()
        {

        }

        public AlpineBitsTypes(Type tin, Type tout)
        {
            RequestType = tin;
            ResponseType = tout;
        }
    }

    public class AlpineBitsActions
    {
        public const string OtaPingHandshaking = "OTA_Ping:Handshaking";

    }

    public class AlpineBitsPingHandler : IAlpineBitsActionHandler<OTA_PingRQ, OTA_PingRS>
    {
        public string Action => AlpineBitsActions.OtaPingHandshaking;

        public OTA_PingRS Work(OTA_PingRQ data)
        {
            return new OTA_PingRS
            {
                TimeStamp = data.TimeStamp,
                Items = new object[] { data.EchoData, new object(), 
                    new OTA_PingRSWarnings() {
                    Warning = [new OTA_PingRSWarningsWarning() {
                        Type = "11",
                        Status = OTA_PingRSWarningsWarningStatus.ALPINEBITS_HANDSHAKE,
                        StatusSpecified = true,
                        Text = [@"
                          {
                            ""versions"": [{
                              ""version"": ""2022-10"",
                              ""actions"": [{
                                    ""action"": ""action_OTA_Ping""
                                  },{
                                    ""action"": ""action_OTA_Read""
                                  }]
                              }]
                          }
                        "]
                    }]
                }}
            };
        }
    }

    // Handler-Registry
    public class AlpineBitsHandlerRegistry
    {
        private readonly Dictionary<string, IAlpineBitsActionHandler> _handlers = new();

        public void Register(IAlpineBitsActionHandler handler)
        {
            _handlers[handler.Action] = handler;
        }

        public IAlpineBitsActionHandler? GetHandler(string action)
        {
            _handlers.TryGetValue(action, out var handler);
            return handler;
        }
    }

    public static Dictionary<string, AlpineBitsTypes> AlpineBitsTypeDic = new()
    {
        { AlpineBitsActions.OtaPingHandshaking, new(typeof(OTA_PingRQ), typeof(OTA_PingRS)) }
    };

    public class AlpineBitsRQ
    {



        private HttpRequest httpRequest { get; set; }

        public string action { get; set; }
        public string request { get; set; }


        public List<IAlpineBitsActionHandler> Handlers { get; private set; } = new();

        public AlpineBitsRQ(HttpRequest request)
        {
            httpRequest = request;
        }

        public async Task<IActionResult> GenerateOutput(CancellationToken cancellationToken = default)
        {
            if (!httpRequest.HasFormContentType)
                throw new Exception("request has to be multiform/data");


            // extract form data to reqeuest and action
            var form = await httpRequest.ReadFormAsync(cancellationToken);
            var action = form["action"].ToString();
            var payload = form["request"].ToString();

            if (string.IsNullOrEmpty(action))
                throw new Exception("no value in action");

            // Handler-Registry initialisieren (sollte in DI verschoben werden)
            var registry = new AlpineBitsHandlerRegistry();
            registry.Register(new AlpineBitsPingHandler());
            // registry.Register(new OtherHandler());

            var handler = registry.GetHandler(action);
            if (handler == null)
            {
                throw new Exception($"Unsupported action '{action}'.");
            }


            // detect input type and output type
            AlpineBitsTypes alpineBitsTypes;
            if (!AlpineBitsTypeDic.TryGetValue(action, out alpineBitsTypes))
                throw new Exception("no handler registered");

            // XML deserialisieren
            var serializer = new XmlSerializer(handler.RequestType);
            using var reader = new StringReader(payload);
            var request = serializer.Deserialize(reader);

            // Handler ausführen
            var response = handler.Process(request!);

            // XML serialisieren
            var responseSerializer = new XmlSerializer(handler.ResponseType);
            using var writer = new StringWriter();
            responseSerializer.Serialize(writer, response);

            
            return Xml(writer.ToString());
        }
        private IActionResult Xml(string xmlData)
        {
            return new ContentResult()
            {
                Content = xmlData,
                ContentType = "application/xml",
                StatusCode = 200,
            };
        }

    }


    



}
