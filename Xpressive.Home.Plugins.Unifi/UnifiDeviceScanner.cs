using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using UnifiApi;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Unifi
{
    internal sealed class UnifiDeviceScanner : BackgroundService
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

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });

            if (!_isValidConfiguration)
            {
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await ScanNetworkAsync();
                }
                catch (Exception e)
                {
                    Log.Fatal(e, e.Message);
                }
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ContinueWith(_ => { });
            }
        }

        private async Task ScanNetworkAsync()
        {
            var url = $"https://{_ipAddress}:{_port}";

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

                    var onlineClients = await client.ListOnlineClientsAsync();

                    foreach (var onlineClient in onlineClients.Data)
                    {
                        var mac = onlineClient.Mac.MacAddressToBytes();
                        var name = onlineClient.Name ?? onlineClient.Hostname ?? onlineClient.Oui;
                        var networkDeviceFoundMessage = new NetworkDeviceFoundMessage("Unifi", onlineClient.Ip, mac, name);
                        networkDeviceFoundMessage.Values.Add("VLAN", onlineClient.Vlan.ToString("D"));
                        networkDeviceFoundMessage.Values.Add("Radio", onlineClient.Radio);
                        networkDeviceFoundMessage.Values.Add("Network", onlineClient.Network);
                        networkDeviceFoundMessage.Values.Add("RadioProto", onlineClient.RadioProto);
                        networkDeviceFoundMessage.Values.Add("IsWired", onlineClient.IsWired.ToString());
                        networkDeviceFoundMessage.Values.Add("Signal", onlineClient.Signal.ToString("D"));
                        _messageQueue.Publish(networkDeviceFoundMessage);
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
