using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xpressive.Home.Plugins.Lifx.Utils;

namespace Xpressive.Home.Plugins.Lifx
{
    internal class LifxLocalClient : IDisposable
    {
        private UdpClient _listeningClient;
        private readonly Dictionary<string, LifxLocalLight> _discoveredBulbs = new Dictionary<string, LifxLocalLight>();
        private static readonly Random _randomizer = new Random();

        public event EventHandler<LifxLocalLight> DeviceDiscovered;
        public event EventHandler<Tuple<LifxLocalLight, string, object>> VariableChanged;

        public IEnumerable<LifxLocalLight> Lights => _discoveredBulbs.Values;

        private async Task Receive(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _listeningClient.ReceiveAsync();
                    HandleIncomingMessages(result.Buffer, result.RemoteEndPoint);
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        public async Task StartDeviceDiscoveryAsync(CancellationToken cancellationToken)
        {
            _listeningClient = new UdpClient(56700);
            _listeningClient.Client.Blocking = false;
            _listeningClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            var _ = Task.Factory.StartNew(() => Receive(cancellationToken));

            var source = (uint) _randomizer.Next(int.MaxValue);
            var header = new FrameHeader
            {
                Identifier = source
            };

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await BroadcastMessageAsync(null, header, MessageType.DeviceGetService, null);
                }
                catch
                {
                }

                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
            }
        }

        private void HandleIncomingMessages(byte[] data, IPEndPoint remoteEndPoint)
        {
            if (IsOwnIpAddress(remoteEndPoint))
            {
                return;
            }

            var msg = ParseMessage(data);

            if (msg.Header.TargetMacAddress.All(b => b == 0))
            {
                return;
            }

            var device = ProcessDeviceDiscoveryMessage(remoteEndPoint, msg);
            ProcessVariables(msg, device);
        }

        private void ProcessVariables(LifxResponse msg, LifxLocalLight light)
        {
            var stateLabelResponse = msg as StateLabelResponse;
            var lightStateResponse = msg as LightStateResponse;
            var lightPowerResponse = msg as LightPowerResponse;

            if (stateLabelResponse != null)
            {
                light.Name = stateLabelResponse.Label;
                VariableChanged?.Invoke(light, Tuple.Create(light, "Name", (object)stateLabelResponse.Label));
            }
            else if (lightStateResponse != null)
            {
                var brightness = lightStateResponse.Brightness/65535d;
                var saturation = lightStateResponse.Saturation/65535d;
                var kelvin = lightStateResponse.Kelvin;
                double hue = lightStateResponse.Hue;

                var color = new HsbkColor
                {
                    Hue = hue,
                    Saturation = saturation,
                    Brightness = brightness,
                    Kelvin = kelvin
                };
                
                var hexColor = color.ToRgb().ToString();

                light.Name = lightStateResponse.Label;
                light.Color = color;
                VariableChanged?.Invoke(light, Tuple.Create(light, "Color", (object)hexColor));
                VariableChanged?.Invoke(light, Tuple.Create(light, "Brightness", (object)Math.Round(brightness, 2)));
                VariableChanged?.Invoke(light, Tuple.Create(light, "Name", (object)lightStateResponse.Label));
                VariableChanged?.Invoke(light, Tuple.Create(light, "IsOn", (object)lightStateResponse.IsOn));
            }
            else if (lightPowerResponse != null)
            {
                VariableChanged?.Invoke(light, Tuple.Create(light, "IsOn", (object)lightPowerResponse.IsOn));
            }
        }

        private bool IsOwnIpAddress(IPEndPoint remoteEndPoint)
        {
            var hostName = Dns.GetHostName();
            var ipEntry = Dns.GetHostEntry(hostName);
            var addr = ipEntry.AddressList;

            return addr.Any(a => a.ToString().Equals(remoteEndPoint.Address.ToString()));
        }

        private LifxLocalLight ProcessDeviceDiscoveryMessage(IPEndPoint remoteAddress, LifxResponse msg)
        {
            LifxLocalLight light;
            if (_discoveredBulbs.TryGetValue(remoteAddress.ToString(), out light))
            {
                light.LastSeen = DateTime.UtcNow;
                return light;
            }

            var device = new LifxLocalLight
            {
                HostName = remoteAddress,
                LastSeen = DateTime.UtcNow,
                Id = string.Join("", msg.Header.TargetMacAddress.Select(b => b.ToString("x2")))
            };
            
            _discoveredBulbs[remoteAddress.ToString()] = device;

            DeviceDiscovered?.Invoke(this, device);
            return device;
        }

        private LifxResponse ParseMessage(byte[] packet)
        {
            using (var ms = new MemoryStream(packet))
            {
                var header = new FrameHeader();

                using (var reader = new EndianBinaryReader(EndianBitConverter.Little, ms, Encoding.UTF8))
                {
                    //frame
                    var size = reader.ReadUInt16();
                    if (packet.Length != size || size < 36)
                    {
                        throw new Exception("Invalid packet");
                    }

                    var a = reader.ReadUInt16(); //origin:2, reserved:1, addressable:1, protocol:12
                    var source = reader.ReadUInt32();
                    //frame address
                    header.TargetMacAddress = reader.ReadBytes(8);
                    ms.Seek(6, SeekOrigin.Current); //skip reserved
                    var b = reader.ReadByte(); //reserved:6, ack_required:1, res_required:1, 
                    header.Sequence = reader.ReadByte();
                    //protocol header
                    var nanoseconds = reader.ReadUInt64();
                    header.AtTime = Utilities.Epoch.AddMilliseconds(nanoseconds * 0.000001);
                    var type = (MessageType)reader.ReadUInt16();
                    ms.Seek(2, SeekOrigin.Current); //skip reserved
                    byte[] payload = null;
                    if (size > 36)
                    {
                        payload = reader.ReadBytes(size - 36);
                    }

                    return LifxResponse.Create(header, type, source, payload);
                }
            }
        }

        private async Task BroadcastMessageAsync(IPEndPoint endpoint, FrameHeader header, MessageType type, params object[] args)
        {
            List<byte> payload = new List<byte>();
            if (args != null)
            {
                foreach (var arg in args)
                {
                    if (arg is ushort)
                        payload.AddRange(BitConverter.GetBytes((ushort)arg));
                    else if (arg is uint)
                        payload.AddRange(BitConverter.GetBytes((uint)arg));
                    else if (arg is byte)
                        payload.Add((byte)arg);
                    else if (arg is byte[])
                        payload.AddRange((byte[])arg);
                    else if (arg is string)
                        payload.AddRange(Encoding.UTF8.GetBytes(((string)arg).PadRight(32).Take(32).ToArray())); //All strings are 32 bytes
                    else
                        throw new NotSupportedException(args.GetType().FullName);
                }
            }

            await BroadcastMessagePayloadAsync(endpoint, header, type, payload.ToArray());
        }

        private async Task BroadcastMessagePayloadAsync(IPEndPoint endpoint, FrameHeader header, MessageType type, byte[] payload)
        {
            if (endpoint == null)
            {
                endpoint = new IPEndPoint(IPAddress.Broadcast, 56700);
            }

            var ms = new MemoryStream();
            var w = new EndianBinaryWriter(EndianBitConverter.Little, ms, Encoding.UTF8);
            w.Write((ushort)((payload?.Length ?? 0) + 36));
            w.Write((ushort)0x3400);
            w.Write(header.Identifier);
            w.Write(header.TargetMacAddress);
            w.Write(new byte[6]); //reserved 1

            if (header.AcknowledgeRequired && header.ResponseRequired)
            {
                w.Write((byte)0x03);
            }
            else if (header.AcknowledgeRequired)
            {
                w.Write((byte)0x02);
            }
            else if (header.ResponseRequired)
            {
                w.Write((byte) 0x01);
            }
            else
            {
                w.Write((byte)0x00);
            }

            w.Write(header.Sequence);

            if (header.AtTime > DateTime.MinValue)
            {
                var time = header.AtTime.ToUniversalTime();
                w.Write((ulong)(time - new DateTime(1970, 01, 01)).TotalMilliseconds * 10);
            }
            else
            {
                w.Write((ulong)0);
            }

            w.Write((ushort)type);
            w.Write((ushort)0);

            if (payload != null)
            {
                w.Write(payload);
            }

            using (var client = new UdpClient())
            {
                var data = ms.ToArray();
                client.DontFragment = true;
                if (endpoint.Address.Equals(IPAddress.Broadcast))
                {
                    client.EnableBroadcast = true;
                }
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Connect(endpoint);

                await client.SendAsync(data, data.Length).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sets the device power state
        /// </summary>
        /// <param name="device"></param>
        /// <param name="isOn"></param>
        /// <returns></returns>
        public async Task SetDevicePowerStateAsync(LifxLocalLight device, bool isOn)
        {
            FrameHeader header = new FrameHeader
            {
                Identifier = (uint)_randomizer.Next(),
                AcknowledgeRequired = true
            };

            await BroadcastMessageAsync(device.HostName, header, MessageType.DeviceSetPower, (ushort)(isOn ? 65535 : 0));
        }

        public async Task SetLightPowerAsync(LifxLocalLight bulb, TimeSpan transitionDuration, bool isOn)
        {
            if (transitionDuration.TotalMilliseconds > uint.MaxValue || transitionDuration.Ticks < 0)
            {
                throw new ArgumentOutOfRangeException("transitionDuration");
            }

            var header = new FrameHeader()
            {
                Identifier = (uint)_randomizer.Next(),
                AcknowledgeRequired = true
            };

            await BroadcastMessageAsync(
                bulb.HostName,
                header,
                MessageType.LightSetPower,
                (ushort)(isOn ? 65535 : 0),
                (ushort)transitionDuration.TotalMilliseconds
            ).ConfigureAwait(false);
        }

        /// <summary>
        /// Sets color and temperature for a bulb and uses a transition time to the provided state
        /// </summary>
        /// <param name="bulb">Light bulb</param>
        /// <param name="hue">0..65535</param>
        /// <param name="saturation">0..65535</param>
        /// <param name="brightness">0..65535</param>
        /// <param name="kelvin">2700..9000</param>
        /// <param name="transitionDuration"></param>
        /// <returns></returns>
        public async Task SetColorAsync(LifxLocalLight bulb,
            ushort hue,
            ushort saturation,
            ushort brightness,
            ushort kelvin,
            TimeSpan transitionDuration)
        {
            if (transitionDuration.TotalMilliseconds > uint.MaxValue ||
                transitionDuration.Ticks < 0)
                throw new ArgumentOutOfRangeException(nameof(transitionDuration));
            if (kelvin < 2500 || kelvin > 9000)
            {
                throw new ArgumentOutOfRangeException(nameof(kelvin), "Kelvin must be between 2500 and 9000");
            }

            System.Diagnostics.Debug.WriteLine("Setting color to {0}", bulb.HostName);
            FrameHeader header = new FrameHeader()
            {
                Identifier = (uint)_randomizer.Next(),
                AcknowledgeRequired = true
            };
            uint duration = (uint)transitionDuration.TotalMilliseconds;

            await BroadcastMessageAsync(bulb.HostName, header,
                MessageType.LightSetColor, (byte)0x00, //reserved
                    hue, saturation, brightness, kelvin, //HSBK
                    duration
            );
        }

        /// <summary>
        /// Gets the current state of the bulb
        /// </summary>
        /// <param name="bulb"></param>
        /// <returns></returns>
        public async Task GetLightStateAsync(LifxLocalLight bulb)
        {
            var header = new FrameHeader
            {
                Identifier = (uint)_randomizer.Next(),
                AcknowledgeRequired = false
            };
            await BroadcastMessageAsync(bulb.HostName, header, MessageType.LightGet);
        }

        /// <summary>
        /// Disposes the client
        /// </summary>
        public void Dispose()
        {
            _listeningClient?.Dispose();
        }
    }

    internal class FrameHeader
    {
        public uint Identifier;
        public byte Sequence;
        public bool AcknowledgeRequired;
        public bool ResponseRequired;
        public byte[] TargetMacAddress;
        public DateTime AtTime;

        public FrameHeader()
        {
            Identifier = 0;
            Sequence = 0;
            AcknowledgeRequired = false;
            ResponseRequired = false;
            TargetMacAddress = new byte[8];
            AtTime = DateTime.MinValue;
        }
    }

    /// <summary>
    /// Base class for LIFX response types
    /// </summary>
    public abstract class LifxResponse
    {
        internal static LifxResponse Create(FrameHeader header, MessageType type, uint source, byte[] payload)
        {
            LifxResponse response = null;
            switch (type)
            {
                case MessageType.DeviceAcknowledgement:
                    response = new AcknowledgementResponse(payload);
                    break;
                case MessageType.DeviceStateLabel:
                    response = new StateLabelResponse(payload);
                    break;
                case MessageType.LightState:
                    response = new LightStateResponse(payload);
                    break;
                case MessageType.LightStatePower:
                    response = new LightPowerResponse(payload);
                    break;
                default:
                    response = new UnknownResponse(payload);
                    break;
            }
            response.Header = header;
            response.Type = type;
            response.Payload = payload;
            response.Source = source;
            return response;
        }
        internal LifxResponse() { }
        internal FrameHeader Header { get; private set; }
        internal byte[] Payload { get; private set; }
        internal MessageType Type { get; private set; }
        internal uint Source { get; private set; }
    }

    /// <summary>
    /// Response to any message sent with ack_required set to 1. 
    /// </summary>
    internal class AcknowledgementResponse : LifxResponse
    {
        internal AcknowledgementResponse(byte[] payload) : base() { }
    }

    /// <summary>
    /// Response to GetLabel message. Provides device label.
    /// </summary>
    internal class StateLabelResponse : LifxResponse
    {
        internal StateLabelResponse(byte[] payload) : base()
        {

            if (payload != null)
                Label = Encoding.UTF8.GetString(payload, 0, payload.Length).Replace("\0", "");
        }
        public string Label { get; private set; }
    }

    /// <summary>
    /// Sent by a device to provide the current light state
    /// </summary>
    public class LightStateResponse : LifxResponse
    {
        internal LightStateResponse(byte[] payload) : base()
        {
            Hue = EndianBitConverter.Little.ToUInt16(payload, 0);
            Saturation = EndianBitConverter.Little.ToUInt16(payload, 2);
            Brightness = EndianBitConverter.Little.ToUInt16(payload, 4);
            Kelvin = EndianBitConverter.Little.ToUInt16(payload, 6);
            var isOn = EndianBitConverter.Little.ToUInt16(payload, 10);
            IsOn = isOn > 0;
            Label = Encoding.UTF8.GetString(payload, 12, 32).Replace("\0", "");
        }
        /// <summary>
        /// Hue
        /// </summary>
        public ushort Hue { get; private set; }
        /// <summary>
        /// Saturation (0=desaturated, 65535 = fully saturated)
        /// </summary>
        public ushort Saturation { get; private set; }
        /// <summary>
        /// Brightness (0=off, 65535=full brightness)
        /// </summary>
        public ushort Brightness { get; private set; }
        /// <summary>
        /// Bulb color temperature
        /// </summary>
        public ushort Kelvin { get; private set; }
        /// <summary>
        /// Power state
        /// </summary>
        public bool IsOn { get; private set; }
        /// <summary>
        /// Light label
        /// </summary>
        public string Label { get; private set; }
    }

    internal class LightPowerResponse : LifxResponse
    {
        internal LightPowerResponse(byte[] payload) : base()
        {
            IsOn = EndianBitConverter.Little.ToUInt16(payload, 0) > 0;
        }
        public bool IsOn { get; private set; }
    }

    internal class UnknownResponse : LifxResponse
    {
        internal UnknownResponse(byte[] payload) : base()
        {
        }
    }

    internal static class Utilities
    {
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    internal enum MessageType : ushort
    {
        //Device Messages
        DeviceGetService = 0x02,
        DeviceStateService = 0x03,
        DeviceGetTime = 0x04,
        DeviceSetTime = 0x05,
        DeviceStateTime = 0x06,
        DeviceGetHostInfo = 12,
        DeviceStateHostInfo = 13,
        DeviceGetHostFirmware = 14,
        DeviceStateHostFirmware = 15,
        DeviceGetWifiInfo = 16,
        DeviceStateWifiInfo = 17,
        DeviceGetWifiFirmware = 18,
        DeviceStateWifiFirmware = 19,
        DeviceGetPower = 20,
        DeviceSetPower = 21,
        DeviceStatePower = 22,
        DeviceGetLabel = 23,
        DeviceSetLabel = 24,
        DeviceStateLabel = 25,
        DeviceGetVersion = 32,
        DeviceStateVersion = 33,
        DeviceGetInfo = 34,
        DeviceStateInfo = 35,
        DeviceAcknowledgement = 45,
        DeviceEchoRequest = 58,
        DeviceEchoResponse = 59,
        //Light messages
        LightGet = 101,
        LightSetColor = 102,
        LightState = 107,
        LightGetPower = 116,
        LightSetPower = 117,
        LightStatePower = 118,


        //Unofficial
        LightGetTemperature = 0x6E,
        //LightStateTemperature = 0x6f,
    }

    /// <summary>
    /// LIFX Generic Device
    /// </summary>
    public sealed class LifxLocalLight
    {
        internal LifxLocalLight() { }

        public IPEndPoint HostName { get; internal set; }
        public DateTime LastSeen { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public HsbkColor Color { get; set; }
    }
}
