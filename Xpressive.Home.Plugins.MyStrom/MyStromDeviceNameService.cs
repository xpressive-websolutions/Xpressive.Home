using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using RestSharp;

namespace Xpressive.Home.Plugins.MyStrom
{
    internal class MyStromDeviceNameService : IMyStromDeviceNameService
    {
        private readonly string _username;
        private readonly string _password;

        public MyStromDeviceNameService()
        {
            _username = ConfigurationManager.AppSettings["mystrom.username"];
            _password = ConfigurationManager.AppSettings["mystrom.password"];
        }

        public async Task<IDictionary<string, string>> GetDeviceNamesByMacAsync()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_password))
            {
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

            foreach (var device in dataResponse.Data[0].ecns)
            {
                result.Add(device.mac, device.name);
            }

            return result;
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