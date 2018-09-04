using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using UnifiApi;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Plugins.Unifi
{
    internal sealed class UnifiDeviceScanner : INetworkDeviceScanner
    {
        private readonly IMessageQueue _messageQueue;
        private readonly string _ipAddress;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly bool _isValidConfiguration = true;

        public UnifiDeviceScanner(IMessageQueue messageQueue, IConfiguration configuration)
        {
            _messageQueue = messageQueue;
            _ipAddress = configuration["unifi:ipaddress"];
            _username = configuration["unifi:username"];
            _password = configuration["unifi:password"];
            int.TryParse(configuration["unifi:port"], out _port);

            if (string.IsNullOrEmpty(_ipAddress))
            {
                messageQueue.Publish(new NotifyUserMessage("Add Unifi configuration (ipaddress) to config file."));
                _isValidConfiguration = false;
            }

            if (_port <= 0 || _port > 65535)
            {
                messageQueue.Publish(new NotifyUserMessage("Add Unifi configuration (port) to config file."));
                _isValidConfiguration = false;
            }

            if (string.IsNullOrEmpty(_username))
            {
                messageQueue.Publish(new NotifyUserMessage("Add Unifi configuration (username) to config file."));
                _isValidConfiguration = false;
            }

            if (string.IsNullOrEmpty(_password))
            {
                messageQueue.Publish(new NotifyUserMessage("Add Unifi configuration (password) to config file."));
                _isValidConfiguration = false;
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { }).ConfigureAwait(false);

            if (!_isValidConfiguration)
            {
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                await ScanNetworkAsync().ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ContinueWith(_ => { }).ConfigureAwait(false);
            }
        }

        private async Task ScanNetworkAsync()
        {
            var url = $"{_ipAddress}:{_port}";

            try
            {
                using (var client = new Client(url, ignoreSslCertificate: true))
                {
                    var loginResult = await client.LoginAsync(_username, _password);

                    if (!loginResult.Result)
                    {
                        Log.Warning("Unable to log in to unifi controller.");
                        return;
                    }

                    var deviceResult = await client.ListDevicesAsync();

                    foreach (var device in deviceResult.Data)
                    {
                        var mac = device.Mac.MacAddressToBytes();
                        var name = device.Name ?? device.DeviceId ?? device.Model;
                        _messageQueue.Publish(new NetworkDeviceFoundMessage("Unifi", device.Ip, mac, name));
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
        }
    }
}
