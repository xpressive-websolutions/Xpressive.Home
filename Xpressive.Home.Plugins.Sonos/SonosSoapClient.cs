using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Polly;

namespace Xpressive.Home.Plugins.Sonos
{
    internal class SonosSoapClient : ISonosSoapClient
    {
        public async Task<XmlDocument> PostRequestAsync(Uri uri, string action, string body)
        {
            try
            {
                return await PostRequestInternal(uri, action, body);
            }
            catch (Exception ex)
            {
                ex.ToString();
                throw;
            }
        }

        public async Task<Dictionary<string, string>> ExecuteAsync(SonosDevice device, UpnpService service, UpnpAction action, Dictionary<string, string> values)
        {
            var uri = new Uri($"http://{device.IpAddress}:1400{service.ControlUrl}");
            var soapAction = $"{service.Id}#{action.Name}";

            var body = new StringBuilder();
            body.Append($"<u:{action.Name} xmlns:u=\"{service.Type}\">");

            foreach (var argument in action.InputArguments)
            {
                string value;
                if (values.TryGetValue(argument, out value))
                {
                    body.Append($"<{argument}>{value}</{argument}>");
                }
            }
            body.Append($"</u:{action.Name}>");

            var document = await PostRequestInternal(uri, soapAction, body.ToString());
            var result = new Dictionary<string, string>();

            foreach (var argument in action.OutputArguments)
            {
                var value = document.SelectSingleNode("//" + argument)?.InnerText;
                result.Add(argument, value);
            }

            return result;
        }

        private async Task<XmlDocument> PostRequestInternal(Uri uri, string action, string body)
        {
            var request = WebRequest.CreateHttp(uri);
            request.Method = "POST";
            request.Headers.Add("SOAPACTION", $"\"{action}\"");
            request.ContentType = "text/xml; charset=\"utf-8\"";

            var message =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
                $"<s:Body>{body}</s:Body>" +
                "</s:Envelope>\n";

            var payload = Encoding.UTF8.GetBytes(message);
            request.ContentLength = payload.Length;

            using (var stream = await request.GetRequestStreamAsync())
            {
                var data = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
            }

            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5)
                });

            return await policy.ExecuteAsync(async () =>
            {
                using (var response = await request.GetResponseAsync())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            await stream.FlushAsync();
                            var xml = await reader.ReadToEndAsync();
                            var document = new XmlDocument();
                            document.LoadXml(SanitizeXmlString(xml));
                            return document;
                        }
                    }
                }
            });
        }

        private static string SanitizeXmlString(string xml)
        {
            var buffer = new StringBuilder(xml.Length);

            foreach (var c in xml.Where(XmlConvert.IsXmlChar))
            {
                buffer.Append(c);
            }

            return buffer.ToString();
        }
    }
}
