using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Plugins.Nmap
{
    internal sealed class NmapDeviceScanner : INetworkDeviceScanner
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(NmapDeviceScanner));
        private static readonly Regex _ipRegex = new Regex(@"\s?\(?(?<ip>[0-9\.]{7,15})\)?", RegexOptions.Compiled, TimeSpan.FromMilliseconds(10));
        private static readonly Regex _deviceRegex = new Regex(@"(?<mac>[0-9a-fA-F\:]{17})(?:\s\((?<manufacturer>[a-zA-Z0-9\s\&\(\)]+)\))?", RegexOptions.Compiled, TimeSpan.FromMilliseconds(10));

        private readonly IMessageQueue _messageQueue;
        private readonly IIpAddressService _ipAddressService;
        private readonly string _nmapLocation;

        public NmapDeviceScanner(IMessageQueue messageQueue, IIpAddressService ipAddressService)
        {
            _messageQueue = messageQueue;
            _ipAddressService = ipAddressService;
            _nmapLocation = ConfigurationManager.AppSettings["nmap.location"];

            if (string.IsNullOrEmpty(_nmapLocation) || !File.Exists(_nmapLocation))
            {
                messageQueue.Publish(new NotifyUserMessage("Add nmap configuration to config file."));
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { }).ConfigureAwait(false);

            if (string.IsNullOrEmpty(_nmapLocation) || !File.Exists(_nmapLocation))
            {
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                await ScanNetworkAsync(cancellationToken);
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ContinueWith(_ => { }).ConfigureAwait(false);
            }
        }

        private async Task ScanNetworkAsync(CancellationToken cancellationToken)
        {
            try
            {
                var ipAddress = string.Join(".", _ipAddressService.GetIpAddress().Split('.').Take(3)) + ".1";
                var lines = await Task.Run(() => GetNmapOutput(_nmapLocation, ipAddress), cancellationToken).ConfigureAwait(false);

                while (lines.Count > 0 && !cancellationToken.IsCancellationRequested)
                {
                    string line1, line2, line3;

                    if (!lines.TryDequeue(out line1) || !lines.TryDequeue(out line2) || !lines.TryDequeue(out line3) || line1.Length < 21 || line3.Length < 13)
                    {
                        continue;
                    }

                    line1 = line1.Substring(21);
                    line3 = line3.Substring(13);

                    var ipMatch = _ipRegex.Match(line1);
                    var deviceMatch = _deviceRegex.Match(line3);

                    if (!ipMatch.Success || !deviceMatch.Success)
                    {
                        continue;
                    }

                    var deviceName = line1.Substring(0, ipMatch.Index);
                    var ip = ipMatch.Groups["ip"].Value;
                    var mac = deviceMatch.Groups["mac"].Value;
                    var manufacturer = deviceMatch.Groups["manufacturer"].Value;

                    _messageQueue.Publish(new NetworkDeviceFoundMessage("NMAP", ip, mac.MacAddressToBytes(), manufacturer: manufacturer, friendlyName: deviceName));
                }
            }
            catch (Exception e)
            {
                _log.Error(e.Message, e);
            }
        }

        private static Queue<string> GetNmapOutput(string nmapLocation, string ipAddress)
        {
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = nmapLocation,
                    Arguments = $"-sP {ipAddress}/24",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            var lines = new Queue<string>();
            while (!proc.StandardOutput.EndOfStream)
            {
                var line = proc.StandardOutput.ReadLine();
                lines.Enqueue(line);
            }

            proc.WaitForExit();

            lines.Dequeue();
            lines.Dequeue();

            return lines;
        }
    }
}
