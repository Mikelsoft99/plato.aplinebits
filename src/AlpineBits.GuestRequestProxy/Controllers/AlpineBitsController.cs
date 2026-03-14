using AlpineBits.GuestRequestProxy.Data.Repositories;
using AlpineBits.GuestRequestProxy.Models;
using AlpineBits.GuestRequestProxy.Models.AlpineBits;
using AlpineBits.GuestRequestProxy.Models.AlpineBits.Capabilities;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using static AlpineBits.GuestRequestProxy.Controllers.AlpineBitsController;

namespace AlpineBits.GuestRequestProxy.Controllers;

[ApiController]
[Route("alpinebits")]
public sealed class AlpineBitsController(
    ITenantRepository tenantRepository,
    IGuestRequestLogRepository logRepository
    ) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [Produces("application/xml")]
    public async Task<IActionResult> Post(CancellationToken cancellationToken)
    {
        AlpineBitsRQ_202410 alpineBitsRQ = new AlpineBitsRQ_202410(Request);

        // map all headers with prefix X- to the output response
        foreach (var header in Request.Headers.Where(h => h.Key.StartsWith("X-", StringComparison.OrdinalIgnoreCase)))
        {
            Response.Headers[header.Key] = header.Value;
        }

        Response.Headers["X-AlpineBits-Server-Accept-Encoding"] = "gzip";
        Response.ContentType = "application/xml";

        var result = await alpineBitsRQ.GenerateOutput(cancellationToken);
        return result;
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

    public class AlpineBitsPingHandler202410() : IAlpineBitsActionHandler<OTA_PingRQ, OTA_PingRS>
    {
        public readonly string Version = "2024-10";

        // capabilities
        public readonly Models.AlpineBits.Capabilities.Action[] SupportedActions =
        [
            new Models.AlpineBits.Capabilities.Action
            {
                action = "action_OTA_Ping"
            },
            new Models.AlpineBits.Capabilities.Action
            {
                action = "action_OTA_Read"
            }
        ];


        public string Action => AlpineBitsActions.OtaPingHandshaking;

        public OTA_PingRS Work(OTA_PingRQ data)
        {
            // extract Echo Data
            string echoData = data.EchoData;

            // parse
            Capabilities capabilities = Capabilities.MapString(echoData);

            // filter current version
            var version = capabilities.versions.FirstOrDefault(v => v.version == Version);
            Capabilities myCapabilitites = new();
            Capabilities filteredEchoData = new();

            if (version != null)
            {
                // prepare output message
                filteredEchoData.versions = capabilities.versions.Where(v => v.version == Version).ToArray();

                // generate the new version with supported actions
                var supportedVersion = version.MatchInput(new Models.AlpineBits.Capabilities.Version() { version = Version, actions = SupportedActions });

                // generate my capabilities based on supported version
                myCapabilitites.versions = supportedVersion != null ? new[] { supportedVersion } : Array.Empty<Models.AlpineBits.Capabilities.Version>();
            }


            return new OTA_PingRS
            {
                TimeStamp = DateTime.Now.ToString("s"),
                Version = "3.000",
                Items = new object[] {
                    new OTA_PingRSWarnings() {
                    Warning = [new OTA_PingRSWarningsWarning() {
                        Type = "11",
                        Status = OTA_PingRSWarningsWarningStatus.ALPINEBITS_HANDSHAKE,
                        StatusSpecified = true,
                        Text = [myCapabilitites.ToString()]
                    }]
                }, 
                    new object(), 
                    //filteredEchoData.ToString()
                    echoData
                }
            };

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

    public class AlpineBitsRQ_202410
    {
        public const string Version = "2024-10";
        public const string HeaderAlpineBitsVersion = "X-AlpineBits-ClientProtocolVersion";

        private HttpRequest httpRequest { get; set; }

        public string action { get; set; }
        public string request { get; set; }

        // UTF-8 für XML-Deklaration erzwingen
        private sealed class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => new UTF8Encoding(false);
        }

        public List<IAlpineBitsActionHandler> Handlers { get; private set; } = new();

        public AlpineBitsRQ_202410(HttpRequest request)
        {
            httpRequest = request;
        }

        public async Task<IActionResult> GenerateOutput(CancellationToken cancellationToken = default)
        {
            if (!httpRequest.HasFormContentType)
                throw new Exception("request has to be multiform/data");

            string headerVersion = httpRequest.Headers[HeaderAlpineBitsVersion].ToString();
            if (headerVersion != Version)
            {
                Console.WriteLine("exit on wrong AlpineBitsVersion " + headerVersion);
                return new StatusCodeResult(400);
            }

            Console.WriteLine("Request with: " + headerVersion);

            var form = await httpRequest.ReadFormAsync(cancellationToken);
            var action = form["action"].ToString();
            var payload = form["request"].ToString();

            if (string.IsNullOrEmpty(action))
            {
                Console.WriteLine("exit on empty action");
                return new StatusCodeResult(400);
            }

            var registry = new AlpineBitsHandlerRegistry();
            registry.Register(new AlpineBitsPingHandler202410());

            var handler = registry.GetHandler(action);
            if (handler == null)
            {
                Console.WriteLine("exit on not supported action: " + action);
                return new StatusCodeResult(400);
            }

            Console.WriteLine("Handling action: " + action);

            AlpineBitsTypes alpineBitsTypes;
            if (!AlpineBitsTypeDic.TryGetValue(action, out alpineBitsTypes))
                throw new Exception("no handler registered");

            var serializer = new XmlSerializer(handler.RequestType);
            using var reader = new StringReader(payload);
            var request = serializer.Deserialize(reader);

            object response = handler.Process(request!);

            var responseSerializer = new XmlSerializer(handler.ResponseType);
            using var writer = new Utf8StringWriter();
            using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings
            {
                Indent = true,
                Encoding = new UTF8Encoding(false)
            }))
            {
                responseSerializer.Serialize(xmlWriter, response);
            }

            string xmlOutput = writer.ToString();
            Console.WriteLine(xmlOutput);
            return Xml(xmlOutput);
        }

        private IActionResult Xml(string xmlData)
        {
            return new ContentResult()
            {
                Content = xmlData,
                ContentType = "application/xml; charset=utf-8",
                StatusCode = 200,
            };
        }

    }






}
