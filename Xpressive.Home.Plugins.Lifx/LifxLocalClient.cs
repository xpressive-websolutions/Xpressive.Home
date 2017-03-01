using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Xpressive.Home.Plugins.Lifx
{
    internal class LifxLocalClient : IDisposable
    {
        private static readonly byte[] _uniqueIdentifier = { 0x14, 0x8b, 0x12, 0x05 };
        private readonly UdpClient _udpClient = new UdpClient(56700);
        private readonly ConcurrentDictionary<string, LifxLocalLight> _bulbs = new ConcurrentDictionary<string, LifxLocalLight>();

        public LifxLocalClient()
        {
            ReceivedPacket += async (sender, message) =>
            {
                var bulbId = string.Join("", message.Address.Target.Select(b => b.ToString("x2")));
                var isNew = false;
                var bulb = _bulbs.AddOrUpdate(
                    bulbId,
                    _ =>
                    {
                        isNew = true;
                        return new LifxLocalLight
                        {
                            Id = bulbId,
                            Mac = message.Address.Target
                        };
                    },
                    (_, b) => b);

                bulb.Endpoint = new IPEndPoint(message.IpAddress, 56700);

                if (isNew)
                {
                    await SendAsync(bulb, new LifxMessageGetColor());
                    DeviceDiscovered?.Invoke(this, bulb);
                }

                var lifxMessageState = message as LifxMessageState;

                if (lifxMessageState != null)
                {
                    bulb.Name = lifxMessageState.Label;
                    bulb.IsOn = lifxMessageState.IsPower;
                    bulb.Color = lifxMessageState.Color;

                    VariableChanged?.Invoke(bulb, Tuple.Create(bulb, "Color", (object)bulb.Color.ToRgb().ToString()));
                    VariableChanged?.Invoke(bulb, Tuple.Create(bulb, "Brightness", (object)Math.Round(bulb.Color.Brightness, 2)));
                    VariableChanged?.Invoke(bulb, Tuple.Create(bulb, "Kelvin", (object)(double)bulb.Color.Kelvin));
                    VariableChanged?.Invoke(bulb, Tuple.Create(bulb, "Name", (object)bulb.Name));
                    VariableChanged?.Invoke(bulb, Tuple.Create(bulb, "IsOn", (object)bulb.IsOn));
                }
            };
        }

        public event EventHandler<LifxMessage> ReceivedPacket;
        public event EventHandler<LifxLocalLight> DeviceDiscovered;
        public event EventHandler<Tuple<LifxLocalLight, string, object>> VariableChanged;

        public IEnumerable<LifxLocalLight> Lights => _bulbs.Values;

        public void StartLifxNetwork(CancellationToken cancellationToken)
        {
            Task.Run(() => Receive(cancellationToken));

            Task.Run(async () =>
            {
                var broadcastAddress = new IPEndPoint(IPAddress.Broadcast, 56700);

                while (!cancellationToken.IsCancellationRequested)
                {
                    await SendAsync(broadcastAddress, new LifxMessageGetService());
                    await SendAsync(broadcastAddress, new LifxMessageGetService { Address = { Sequence = 1 } });
                    await SendAsync(broadcastAddress, new LifxMessageGetColor { Frame = { Tagged = true } });

                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ContinueWith(_ => { });
                }
            });
        }

        public async Task SendAsync(LifxLocalLight bulb, LifxMessage message)
        {
            bulb.Mac.CopyTo(message.Address.Target, 0);

            await SendAsync(bulb.Endpoint, message);
        }

        public async Task SetPowerAsync(LifxLocalLight bulb, bool isOn)
        {
            //await SendAsync(device, new LifxMessageSetPower(isOn));
            await SendAsync(bulb, new LifxMessageSetPower(isOn, TimeSpan.FromMilliseconds(200)));
        }

        public async Task SetPowerAsync(LifxLocalLight bulb, TimeSpan transitionDuration, bool isOn)
        {
            await SendAsync(bulb, new LifxMessageSetPower(isOn, transitionDuration));
        }

        public async Task SetColorAsync(LifxLocalLight bulb, HsbkColor color, TimeSpan transitionDuration)
        {
            var duration = (uint)Math.Max(0, Math.Min(uint.MaxValue, transitionDuration.TotalMilliseconds));
            await SendAsync(bulb, new LifxMessageSetColor(color, duration));
        }

        public async Task GetLightStateAsync(LifxLocalLight bulb)
        {
            await SendAsync(bulb, new LifxMessageGetColor());
        }

        private async Task SendAsync(IPEndPoint endpoint, LifxMessage message)
        {
            var start = DateTime.UtcNow;
            _uniqueIdentifier.CopyTo(message.Frame.Source, 0);
            var data = message.Serialize();
            await _udpClient.SendAsync(data, data.Length, new IPEndPoint(endpoint.Address, 56700));
            var end = DateTime.UtcNow;

            var milliseconds = (end - start).TotalMilliseconds;
            if (milliseconds < 50)
            {
                await Task.Delay((int)(50 - milliseconds));
            }
        }

        private async void Receive(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await _udpClient.ReceiveAsync();

                    if (result.Buffer.Length <= 0)
                    {
                        continue;
                    }

                    if (result.RemoteEndPoint.Port != 56700 || result.Buffer.Length < 36)
                    {
                        continue;
                    }

                    var message = LifxMessageFactory.Deserialize(result.Buffer);

                    if (message != null)
                    {
                        message.IpAddress = result.RemoteEndPoint.Address;
                        OnReceivedPacket(message);
                    }
                }
            }
            catch (Exception e) { }
        }

        private void OnReceivedPacket(LifxMessage e)
        {
            var handler = ReceivedPacket;
            handler?.Invoke(null, e);
        }

        public void Dispose()
        {
            _udpClient?.Close();
        }
    }
}
