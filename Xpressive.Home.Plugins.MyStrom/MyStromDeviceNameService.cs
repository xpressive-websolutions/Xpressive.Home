using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Nito.AsyncEx;
using RestSharp;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.MyStrom
{
    internal class MyStromDeviceNameService : IMyStromDeviceNameService
    {
        private readonly IMessageQueue _messageQueue;
        private readonly string _username;
        private readonly string _password;
        private readonly AsyncLock _lock = new AsyncLock();
        private DateTime _recentResultTimestamp;
        private IDictionary<string, string> _recentResult;

        public MyStromDeviceNameService(IMessageQueue messageQueue)
        {
            _messageQueue = messageQueue;
            _username = ConfigurationManager.AppSettings["mystrom.username"];
            _password = ConfigurationManager.AppSettings["mystrom.password"];

            _recentResultTimestamp = DateTime.MinValue;
            _recentResult = new Dictionary<string, string>(0);
        }

        public async Task<IDictionary<string, string>> GetDeviceNamesByMacAsync()
        {
            using (await _lock.LockAsync())
            {
                if ((DateTime.UtcNow - _recentResultTimestamp).TotalMinutes < 10)
                {
                    return _recentResult;
                }

                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
                {
                    _messageQueue.Publish(new NotifyUserMessage("Add mystrom configuration to config file."));
                    _recentResultTimestamp = DateTime.UtcNow;
                    return result;
                }

                var client = new RestClient("https://mystrom.ch");
                var request = new RestRequest("app/de/auth/login");
                request.AddParameter("email", _username);
                request.AddParameter("password", _password);
                request.AddParameter("remember", "false");

                var response = await client.ExecutePostTaskAsync(request);
                var cookies = response.Cookies;

                request = new RestRequest("app/de/device/command");
                request.AddHeader("Origin", "https://mystrom.ch");
                request.AddHeader("Referer", "https://mystrom.ch/app/de/home");
                request.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.87 Safari/537.36");
                request.AddHeader("Accept", "application/json, text/javascript, */*; q=0.01");
                request.AddHeader("Content-Type", "application/json; charset=UTF-8");
                request.AddHeader("X-Requested-With", "XMLHttpRequest");
                request.AddJsonBody(new[] { new { type = "GetEcns" } });

                foreach (var cookie in cookies)
                {
                    request.AddCookie(cookie.Name, cookie.Value);
                }

                var dataResponse = await client.ExecutePostTaskAsync<List<EcnsDto>>(request);

                if (dataResponse.Data != null && dataResponse.Data.Count > 0)
                {
                    foreach (var device in dataResponse.Data[0].ecns)
                    {
                        result.Add(device.mac, device.name);
                    }
                }
                else
                {
                    // TODO: retry
                    dataResponse.ToString();
                }

                _recentResult = new Dictionary<string, string>(result, StringComparer.OrdinalIgnoreCase);
                _recentResultTimestamp = DateTime.UtcNow;

                return result;
            }
        }

        private class EcnsDto
        {
            public List<DeviceDto> ecns { get; set; }
        }

        private class DeviceDto
        {
            public string mac { get; set; }
            public string name { get; set; }
        }
    }
}