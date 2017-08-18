using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using log4net;
using Nito.AsyncEx;
using Rssdp;
using Xpressive.Home.Contracts.Messaging;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Services
{
    internal sealed class UpnpDeviceDiscoveringService : INetworkDeviceScanner
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(UpnpDeviceDiscoveringService));
        private static readonly ConcurrentDictionary<string, DateTime> _occurrences = new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private static readonly AsyncLock _lock = new AsyncLock();
        private readonly IMessageQueue _messageQueue;

        public UpnpDeviceDiscoveringService(IMessageQueue messageQueue)
        {
            _messageQueue = messageQueue;
        }

        public async Task StartAsync(CancellationToken token)
        {
            var runningTask = Task.Run(async () => { await Task.Delay(TimeSpan.MaxValue, token).ContinueWith(_ => { }); });

            await Task.Delay(TimeSpan.FromSeconds(5), token).ContinueWith(_ => { });

            using (var deviceLocator = new SsdpDeviceLocator())
            {
                while (!token.IsCancellationRequested)
                {
                    Task<IEnumerable<DiscoveredSsdpDevice>> searchTask;

                    try
                    {
                        searchTask = deviceLocator.SearchAsync("upnp:rootdevice", TimeSpan.FromSeconds(10));
                        await Task.WhenAny(runningTask, searchTask);

                        if (searchTask.IsFaulted)
                        {
                            continue;
                        }
                    }
                    catch (Exception e)
                    {
                        _log.Error(e.Message, e);
                        continue;
                    }

                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    var devices = searchTask.Result;

                    if (devices == null)
                    {
                        continue;
                    }

                    foreach (var device in devices)
                    {
                        await UpnpDeviceFound(device);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(60), token).ContinueWith(_ => { });
                }
            }
        }

        private async Task UpnpDeviceFound(DiscoveredSsdpDevice device)
        {
            try
            {
                UpnpDeviceResponse response;

                using (await _lock.LockAsync())
                {
                    response = await CreateUpnpDeviceAsync(device);

                    if (response == null)
                    {
                        return;
                    }

                    var key = $"{device.DescriptionLocation.Host}/{response.Usn}";

                    DateTime lastOccurrence;
                    if (_occurrences.TryGetValue(key, out lastOccurrence))
                    {
                        if ((DateTime.UtcNow - lastOccurrence) < TimeSpan.FromMinutes(5))
                        {
                            return;
                        }
                    }

                    _occurrences.AddOrUpdate(key, DateTime.UtcNow, (k, v) => DateTime.UtcNow);
                }

                var message = new NetworkDeviceFoundMessage("UPNP", response.IpAddress, new byte[0], response.FriendlyName);
                message.Values.Add("Location", response.Location);
                message.Values.Add("Manufacturer", response.Manufacturer);
                message.Values.Add("ModelName", response.ModelName);
                message.Values.Add("Server", response.Server);
                message.Values.Add("USN", response.Usn);

                foreach (var pair in response.OtherHeaders)
                {
                    if (!message.Values.ContainsKey(pair.Key))
                    {
                        message.Values.Add(pair);
                    }
                }

                _messageQueue.Publish(message);
            }
            catch (HttpRequestException)
            {
            }
            catch (WebException)
            {
            }
            catch (TaskCanceledException)
            {
                _log.Error($"TaskCanceledException for device {device.DescriptionLocation.OriginalString}");
            }
            catch (XmlException e)
            {
                _log.Error($"Xml exception in {device.DescriptionLocation.OriginalString}: {e.Message}");
            }
            catch (Exception e)
            {
                _log.Error(e.Message, e);
            }
        }

        private async Task<UpnpDeviceResponse> CreateUpnpDeviceAsync(DiscoveredSsdpDevice device)
        {
            var info = await device.GetDeviceInfo();
            var headers = device.ResponseHeaders?.ToDictionary(
                h => h.Key,
                h => string.Join(" ", h.Value),
                StringComparer.OrdinalIgnoreCase);

            headers = headers ?? new Dictionary<string, string>(0);

            string server;
            string location;
            string usn = info.Udn;

            if (!headers.TryGetValue("location", out location))
            {
                location = device.DescriptionLocation.AbsoluteUri;
            }

            if (!headers.TryGetValue("server", out server))
            {
                server = string.Empty;
            }

            var response = new UpnpDeviceResponse(location, server, usn)
            {
                FriendlyName = info.FriendlyName,
                Manufacturer = info.Manufacturer,
                ModelName = info.ModelName
            };

            foreach (var keyValuePair in headers)
            {
                response.AddHeader(keyValuePair.Key, keyValuePair.Value);
            }

            return response;
        }
    }
}
