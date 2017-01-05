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
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Services
{
    internal sealed class UpnpDeviceDiscoveringService : IUpnpDeviceDiscoveringService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(UpnpDeviceDiscoveringService));
        private static readonly ConcurrentDictionary<string, DateTime> _occurrences = new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private static readonly AsyncLock _lock = new AsyncLock();
        private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();

        public event EventHandler<IUpnpDeviceResponse> DeviceFound;

        public async Task StartDiscoveringAsync()
        {
            var runningTask = Task.Run(async () => { await Task.Delay(TimeSpan.MaxValue, _cancellationToken.Token).ContinueWith(_ => { }); });

            await Task.Delay(TimeSpan.FromSeconds(5), _cancellationToken.Token).ContinueWith(_ => { });

            using (var deviceLocator = new SsdpDeviceLocator())
            {
                deviceLocator.StartListeningForNotifications();
                deviceLocator.DeviceAvailable += async (s, e) => { await UpnpDeviceFound(e.DiscoveredDevice); };

                while (!_cancellationToken.IsCancellationRequested)
                {
                    Task<IEnumerable<DiscoveredSsdpDevice>> searchTask;

                    try
                    {
                        searchTask = deviceLocator.SearchAsync("upnp:rootdevice", TimeSpan.FromSeconds(10));
                        await Task.WhenAny(runningTask, searchTask);
                    }
                    catch (Exception e)
                    {
                        _log.Error(e.Message, e);
                        continue;
                    }

                    if (_cancellationToken.IsCancellationRequested)
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

                    await Task.Delay(TimeSpan.FromSeconds(60), _cancellationToken.Token).ContinueWith(_ => { });
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

                OnDeviceFound(response);
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
                h => h.Key, h => string.Join(" ", h.Value), StringComparer.OrdinalIgnoreCase);

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

        private void OnDeviceFound(IUpnpDeviceResponse device)
        {
            DeviceFound?.Invoke(this, device);
        }

        public void Dispose()
        {
            _cancellationToken.Cancel();
            _cancellationToken.Dispose();
        }
    }
}
