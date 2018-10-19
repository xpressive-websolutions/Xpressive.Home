using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Services;

namespace Xpressive.Home.Plugins.WebHook
{
    internal sealed class WebHookGateway : GatewayBase
    {
        private readonly IWebHookService _webHookService;

        public WebHookGateway(IWebHookService webHookService, IDevicePersistingService persistingService)
            : base("WebHook", true, persistingService)
        {
            _webHookService = webHookService;
        }

        public override IDevice CreateEmptyDevice()
        {
            return new WebHookDevice();
        }

        public override IEnumerable<IAction> GetActions(IDevice device)
        {
            yield break;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });

            await LoadDevicesAsync((id, name) => new WebHookDevice { Id = id, Name = name });
        }

        protected override bool AddDeviceInternal(DeviceBase device)
        {
            if (device == null)
            {
                return false;
            }

            var id = _webHookService.GenerateId();

            if (string.IsNullOrEmpty(device.Id))
            {
                device.Id = id;
            }

            if (!device.IsConfigurationValid())
            {
                return false;
            }

            _webHookService.RegisterNewWebHookAsync(Name, id, device);

            return base.AddDeviceInternal(device);
        }

        protected override Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
        {
            throw new NotSupportedException();
        }
    }
}
