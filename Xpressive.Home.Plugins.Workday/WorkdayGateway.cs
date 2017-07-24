using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xpressive.Home.Contracts.Gateway;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.Plugins.Workday
{
    internal class WorkdayGateway : GatewayBase
    {
        private readonly IMessageQueue _messageQueue;
        private readonly IWorkdayCalculator _calculator;

        public WorkdayGateway(IMessageQueue messageQueue, IWorkdayCalculator calculator) : base("Workday")
        {
            _messageQueue = messageQueue;
            _calculator = calculator;
            _canCreateDevices = true;
        }

        public override IDevice CreateEmptyDevice()
        {
            return new WorkdayDevice();
        }

        public IEnumerable<WorkdayDevice> GetDevices()
        {
            return Devices.OfType<WorkdayDevice>();
        }

        public override IEnumerable<IAction> GetActions(IDevice device)
        {
            yield break;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ContinueWith(_ => { });

            await LoadDevicesAsync((id, name) => new WorkdayDevice { Id = id, Name = name });

            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var device in GetDevices())
                {
                    UpdateVariables(device);
                }

                await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken).ContinueWith(_ => { });
            }
        }

        protected override Task ExecuteInternalAsync(IDevice device, IAction action, IDictionary<string, string> values)
        {
            throw new NotSupportedException();
        }

        private void UpdateVariables(WorkdayDevice device)
        {
            var workdays = _calculator.GetWorkdays(device, DateTime.Today, DateTime.Today.AddDays(1)).ToList();
            var holidays = _calculator.GetHolidays(device, DateTime.Today, DateTime.Today.AddDays(1)).ToList();

            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "TodayIsWorkday", workdays.Contains(DateTime.Today)));
            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "TomorrowIsWorkday", workdays.Contains(DateTime.Today.AddDays(1))));

            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "TodayIsHoliday", holidays.Contains(DateTime.Today)));
            _messageQueue.Publish(new UpdateVariableMessage(Name, device.Id, "TomorrowIsHoliday", holidays.Contains(DateTime.Today.AddDays(1))));
        }
    }
}
