using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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