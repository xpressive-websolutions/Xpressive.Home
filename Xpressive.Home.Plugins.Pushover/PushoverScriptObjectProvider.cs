using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Net;
using log4net;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.Pushover
{
    internal sealed class PushoverScriptObjectProvider : IScriptObjectProvider
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PushoverScriptObjectProvider));

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield return Tuple.Create("pushover", (object)new PushoverScriptObject());
        }

        public IEnumerable<Tuple<string, Delegate>> GetDelegates()
        {
            yield break;
        }

        public class PushoverScriptObject
        {
            private readonly string _token;

            public PushoverScriptObject()
            {
                _token = ConfigurationManager.AppSettings["pushover.token"];
            }

            public async void send(string body, string userKey)
            {
                if (string.IsNullOrEmpty(_token))
                {
                    _log.Error("Unable to send push notification because 'pushover.token' is not specified.");
                    return;
                }

                if (string.IsNullOrEmpty(body) || body.Length > 1024)
                {
                    _log.Error("Unable to send push notification because body is null or longer than 1024 characters.");
                    return;
                }

                using (var client = new WebClient())
                {
                    var data = new NameValueCollection();
                    data["token"] = _token;
                    data["user"] = userKey;
                    data["message"] = body;
                    await client.UploadValuesTaskAsync("https://api.pushover.net/1/messages.json", data);
                }
            }
        }
    }
}
