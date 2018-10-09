using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using RestSharp;
using Serilog;
using Xpressive.Home.Contracts.Automation;

namespace Xpressive.Home.Plugins.Sms
{
    internal sealed class SmsScriptObjectProvider : IScriptObjectProvider
    {
        private readonly IConfiguration _configuration;

        public SmsScriptObjectProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IEnumerable<Tuple<string, Delegate>> GetDelegates()
        {
            yield break;
        }

        public IEnumerable<Tuple<string, object>> GetObjects()
        {
            yield return Tuple.Create("sms", (object)new SmsScriptObject(_configuration));
        }

        public class SmsScriptObject
        {
            private readonly string _username;
            private readonly string _password;
            private readonly Regex _numberValidator;

            public SmsScriptObject(IConfiguration configuration)
            {
                _username = configuration["sms.username"];
                _password = configuration["sms.password"];
                _numberValidator = new Regex(@"\+\d+", RegexOptions.Compiled | RegexOptions.Singleline);
            }

            public void send(string recipient, string text)
            {
                if (string.IsNullOrEmpty(text) || text.Length > 603)
                {
                    Log.Error("Unable to send sms because text is null or longer than 603 characters.");
                    return;
                }

                if (string.IsNullOrEmpty(recipient) || !_numberValidator.IsMatch(recipient))
                {
                    Log.Error("Unable to send sms because recipient isn't a valid phone number.");
                    return;
                }

                if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
                {
                    Log.Error("Unable to send sms because 'sms.username' or 'sms.password' is not specified.");
                    return;
                }

                var client = new RestClient("https://json.aspsms.com");
                var request = new RestRequest("SendSimpleTextSMS");
                request.AddJsonBody(new
                {
                    UserName = _username,
                    Password = _password,
                    Originator = "Xpressive.H",
                    Recipients = new[] { recipient },
                    MessageText = text
                });

                var response = client.Post(request);

                if (response.StatusCode != HttpStatusCode.OK && response.ErrorMessage != null)
                {
                    if (response.ErrorException != null)
                    {
                        Log.Error("Error when sending SMS: " + response.ErrorMessage, response.ErrorException);
                    }
                    else
                    {
                        Log.Error("Error when sending SMS: " + response.ErrorMessage);
                    }
                }
            }
        }
    }
}
