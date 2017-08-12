using System;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using PrimS.Telnet;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Plugins.ZyxelUsg
{
    internal sealed class ZyxelUsgDeviceScanner : INetworkDeviceScanner
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ZyxelUsgDeviceScanner));

        private static readonly Regex _ipAndMacExtractor = new Regex(
            @"(?<ip>[0-9\.]+)\s+[a-z]+\s+(?<mac>([0-9a-f\:]){17})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline,
            TimeSpan.FromSeconds(1));

        private readonly IMessageQueue _messageQueue;
        private readonly string _ipAddress;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly bool _isValidConfiguration = true;

        public ZyxelUsgDeviceScanner(IMessageQueue messageQueue)
        {
            _messageQueue = messageQueue;
            _ipAddress = ConfigurationManager.AppSettings["zyxelusg.ipaddress"];
            _username = ConfigurationManager.AppSettings["zyxelusg.username"];
            _password = ConfigurationManager.AppSettings["zyxelusg.password"];
            int.TryParse(ConfigurationManager.AppSettings["zyxelusg.port"], out _port);

            if (string.IsNullOrEmpty(_ipAddress))
            {
                messageQueue.Publish(new NotifyUserMessage("Add zyxel usg configuration (ipaddress) to config file."));
                _isValidConfiguration = false;
            }

            if (_port <= 0 || _port > 65535)
            {
                messageQueue.Publish(new NotifyUserMessage("Add zyxel usg configuration (port) to config file."));
                _isValidConfiguration = false;
            }

            if (string.IsNullOrEmpty(_username))
            {
                messageQueue.Publish(new NotifyUserMessage("Add zyxel usg configuration (username) to config file."));
                _isValidConfiguration = false;
            }

            if (string.IsNullOrEmpty(_password))
            {
                messageQueue.Publish(new NotifyUserMessage("Add zyxel usg configuration (password) to config file."));
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
                await ScanNetworkAsync(cancellationToken).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ContinueWith(_ => { }).ConfigureAwait(false);
            }
        }

        private async Task ScanNetworkAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (var client = new Client(_ipAddress, _port, cancellationToken))
                {
                    await ReadAsync(client).ConfigureAwait(false);
                    await client.WriteLine(_username).ConfigureAwait(false);
                    await ReadAsync(client).ConfigureAwait(false);
                    await client.WriteLine(_password).ConfigureAwait(false);
                    await ReadAsync(client).ConfigureAwait(false);
                    await client.WriteLine("show arp-table").ConfigureAwait(false);

                    var text = await ReadAsync(client).ConfigureAwait(false);
                    var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        var match = _ipAndMacExtractor.Match(line);

                        if (match.Success)
                        {
                            var ip = match.Groups["ip"].Value;
                            var mac = match.Groups["mac"].Value;
                            var macAddress = mac.MacAddressToBytes();

                            _messageQueue.Publish(new NetworkDeviceFoundMessage("Telnet", ip, macAddress));
                        }
                    }

                    await client.WriteLine("exit").ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _log.Error(e.Message, e);
            }
        }

        private static async Task<string> ReadAsync(Client client)
        {
            string previous, text = null;

            do
            {
                previous = text;
                text = await client.ReadAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            } while (!string.IsNullOrEmpty(text));

            return previous?.Replace("\0", string.Empty).Trim();
        }
    }
}
