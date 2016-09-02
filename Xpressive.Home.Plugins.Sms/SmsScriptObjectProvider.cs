using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text.RegularExpressions;
using log4net;
using RestSharp;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.Sms
{
    internal sealed class SmsScriptObjectProvider : IScriptObjectProvider
    {
        public IEnumerable<Tuple<string, Delegate>> GetDelegates()
        {
            yield break;
        }

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield return Tuple.Create("sms", (object)new SmsScriptObject());
        }

        public class SmsScriptObject
        {
            private static readonly ILog _log = LogManager.GetLogger(typeof(SmsScriptObject));
            private readonly string _username;
            private readonly string _password;
            private readonly Regex _numberValidator;

            public SmsScriptObject()
            {
                _username = ConfigurationManager.AppSettings["sms.username"];
                _password = ConfigurationManager.AppSettings["sms.password"];
                _numberValidator = new Regex(@"\+\d+", RegexOptions.Compiled | RegexOptions.Singleline);
            }

            public void send(string recipient, string text)
            {
                if (string.IsNullOrEmpty(text) || text.Length > 603)
                {
                    _log.Error("Unable to send sms because text is null or longer than 603 characters.");
                    return;
                }

                if (string.IsNullOrEmpty(recipient) || !_numberValidator.IsMatch(recipient))
                {
                    _log.Error("Unable to send sms because recipient isn't a valid phone number.");
                    return;
                }

                if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
                {
                    _log.Error("Unable to send sms because 'sms.username' or 'sms.password' is not specified.");
                    return;
                }

                var client = new RestClient("https://json.aspsms.com");
                var request = new RestRequest("SendSimpleTextSMS");
                request.AddBody(new
                {
                    UserName = _username,
                    Password = _password,
                    Originator = "Xpressive.H",
                    Recipients = new [] { recipient },
                    MessageText = text
                });

                var response = client.Post(request);
                // validate response
            }
        }
    }
}
