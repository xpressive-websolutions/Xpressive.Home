using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Rssdp;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Services
{
    internal sealed class UpnpDeviceDiscoveringService : IUpnpDeviceDiscoveringService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(UpnpDeviceDiscoveringService));
        private bool _isRunning = true;

        public event EventHandler<IUpnpDeviceResponse> DeviceFound;

        public async Task StartDiscoveringAsync()
        {
            var runningTask = Task.Run(async () => { await TaskHelper.DelayAsync(TimeSpan.MaxValue, () => _isRunning); });

            await TaskHelper.DelayAsync(TimeSpan.FromSeconds(5), () => _isRunning);

            using (var deviceLocator = new SsdpDeviceLocator())
            {
                while (_isRunning)
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

                    if (!_isRunning)
                    {
                        break;
                    }

                    var devices = searchTask.Result;
                    var responses = new Dictionary<string, UpnpDeviceResponse>(StringComparer.OrdinalIgnoreCase);

                    foreach (var device in devices)
                    {
                        try
                        {
                            var response = await CreateUpnpDeviceAsync(device);
                            var key = $"{device.DescriptionLocation.Host}/{response.Usn}";
                            responses[key] = response;
                        }
                        catch (Exception e)
                        {
                            _log.Error(e.Message, e);
                        }
                    }

                    foreach (var response in responses.Values)
                    {
                        OnDeviceFound(response);
                    }

                    await TaskHelper.DelayAsync(TimeSpan.FromSeconds(60), () => _isRunning);
                }
            }
        }

        private async Task<UpnpDeviceResponse> CreateUpnpDeviceAsync(DiscoveredSsdpDevice device)
        {
            var info = await device.GetDeviceInfo();
            var headers = device.ResponseHeaders.ToDictionary(
                h => h.Key, h => string.Join(" ", h.Value), StringComparer.OrdinalIgnoreCase);

            string server;
            string location;
            string usn = info.Udn;

            if (!headers.TryGetValue("server", out server) ||
                !headers.TryGetValue("location", out location))
            {
                return null;
            }

            var response = new UpnpDeviceResponse(location, server, usn);

            foreach (var keyValuePair in device.ResponseHeaders)
            {
                var value = string.Join(" ", keyValuePair.Value);
                response.AddHeader(keyValuePair.Key, value);
            }

            return response;
        }

        private void OnDeviceFound(IUpnpDeviceResponse device)
        {
            DeviceFound?.Invoke(this, device);
        }

        public void Dispose()
        {
            _isRunning = false;
        }
    }
}
