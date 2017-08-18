using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Net;
using log4net;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.Pushalot
{
    internal sealed class PushalotScriptObjectProvider : IScriptObjectProvider
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PushalotScriptObjectProvider));

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield return Tuple.Create("pushalot", (object) new PushalotScriptObject());
        }

        public IEnumerable<Tuple<string, Delegate>> GetDelegates()
        {
            yield break;
        }

        public class PushalotScriptObject
        {
            private readonly string _token;

            public PushalotScriptObject()
            {
                _token = ConfigurationManager.AppSettings["pushalot.token"];
            }

            public async void send(string body)
            {
                if (string.IsNullOrEmpty(_token))
                {
                    _log.Error("Unable to send push notification because 'pushalot.token' is not specified.");
                    return;
                }

                if (string.IsNullOrEmpty(body) || body.Length > 32768)
                {
                    _log.Error("Unable to send push notification because body is null or longer than 32768 characters.");
                    return;
                }

                try
                {
                    using (var client = new WebClient())
                    {
                        var data = new NameValueCollection();
                        data["AuthorizationToken"] = _token;
                        data["Body"] = body;
                        await client.UploadValuesTaskAsync("https://pushalot.com/api/sendmessage", data);
                    }
                }
                catch (Exception e)
                {
                    _log.Error(e.Message, e);
                }
            }
        }
    }
}
