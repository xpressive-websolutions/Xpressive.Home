using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Net;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.Pushalot
{
    internal sealed class PushalotScriptObjectProvider : IScriptObjectProvider
    {
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
                using (var client = new WebClient())
                {
                    var data = new NameValueCollection();
                    data["AuthorizationToken"] = _token;
                    data["Body"] = body;
                    await client.UploadValuesTaskAsync("https://pushalot.com/api/sendmessage", data);
                }
            }
        }
    }
}