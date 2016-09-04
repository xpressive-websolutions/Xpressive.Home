using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rssdp;
using Xpressive.Home.Contracts;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Services
{
    public class UpnpDeviceDiscoveringService : IUpnpDeviceDiscoveringService
    {
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
                    var searchTask = deviceLocator.SearchAsync(TimeSpan.FromSeconds(10));
                    await Task.WhenAny(runningTask, searchTask);

                    if (!_isRunning)
                    {
                        break;
                    }

                    var devices = searchTask.Result;
                    var responses = new List<UpnpDeviceResponse>();

                    foreach (var device in devices)
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
                            continue;
                        }

                        if (responses.Any(r => r.Usn.Equals(usn)))
                        {
                            continue;
                        }

                        var response = new UpnpDeviceResponse(location, server, usn);
                        responses.Add(response);

                        foreach (var keyValuePair in device.ResponseHeaders)
                        {
                            var value = string.Join(" ", keyValuePair.Value);
                            response.AddHeader(keyValuePair.Key, value);
                        }
                    }

                    foreach (var response in responses)
                    {
                        OnDeviceFound(response);
                    }

                    await TaskHelper.DelayAsync(TimeSpan.FromSeconds(60), () => _isRunning);
                }
            }
        }

        protected virtual void OnDeviceFound(IUpnpDeviceResponse device)
        {
            DeviceFound?.Invoke(this, device);
        }

        public void Dispose()
        {
            _isRunning = false;
        }
    }
}
