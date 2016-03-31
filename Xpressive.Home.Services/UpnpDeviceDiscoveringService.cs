using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Services
{
    public class UpnpDeviceDiscoveringService : IUpnpDeviceDiscoveringService
    {
        public event EventHandler<IUpnpDeviceResponse> DeviceFound;

        public async Task StartDiscoveringAsync()
        {
            const string multicastIpAddress = "239.255.255.250";
            const int multicastPort = 1900;

            await Task.Delay(TimeSpan.FromSeconds(5));

            while (true)
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                    var payload =
                        "M-SEARCH * HTTP/1.1\r\n" +
                        $"HOST: {multicastIpAddress}:{multicastPort}\r\n" +
                        "ST:upnp:rootdevice\r\n" +
                        "MAN:\"ssdp:discover\"\r\n" +
                        "MX:5\r\n\r\n";
                    var data = Encoding.ASCII.GetBytes(payload);
                    var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(5);
                    var buffer = new byte[16000];

                    socket.SendTo(data, new IPEndPoint(IPAddress.Parse(multicastIpAddress), multicastPort));

                    while (DateTime.UtcNow < timeout)
                    {
                        await Task.Delay(10);

                        if (socket.Available > 0)
                        {
                            var length = socket.Receive(buffer);
                            var response = Encoding.ASCII.GetString(buffer, 0, length).ToLowerInvariant();
                            HandleResponse(response);
                        }
                    }

                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }

                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }

        private void HandleResponse(string response)
        {
            var lines = response.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in lines.Skip(1))
            {
                var pair = line.Split(new[] {':'}, 2);

                if (pair.Length == 2)
                {
                    dict.Add(pair[0], pair[1].Trim());
                }
            }

            var location = dict["location"];
            var server = dict["server"];
            var st = dict["st"];
            var usn = dict["usn"];

            var device = new UpnpDeviceResponse(location, server, st, usn);

            foreach (var pair in dict)
            {
                if (pair.Key.Equals("location", StringComparison.OrdinalIgnoreCase) ||
                    pair.Key.Equals("server", StringComparison.OrdinalIgnoreCase) ||
                    pair.Key.Equals("st", StringComparison.OrdinalIgnoreCase) ||
                    pair.Key.Equals("usn", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                device.AddHeader(pair.Key, pair.Value);
            }

            OnDeviceFound(device);
        }

        protected virtual void OnDeviceFound(IUpnpDeviceResponse device)
        {
            DeviceFound?.Invoke(this, device);
        }
    }
}