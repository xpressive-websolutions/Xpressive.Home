using System;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.WebApi.Controllers
{
    [HubName("notificationHub")]
    public class UserNotificationHub : Hub, IMessageQueueListener<NotifyUserMessage>
    {
        private static readonly List<UserNotificationDto> _notifications = new List<UserNotificationDto>();
        private static readonly object _lock = new object();

        public void Notify(NotifyUserMessage message)
        {
            lock (_lock)
            {
                var dto = new UserNotificationDto
                {
                    Timestamp = DateTime.UtcNow,
                    Message = message.Notification
                };

                _notifications.RemoveAll(n => n.Message.Equals(message.Notification, StringComparison.Ordinal));
                _notifications.Add(dto);

                var context = GlobalHost.ConnectionManager.GetHubContext<UserNotificationHub>();
                context.Clients?.All?.onNotification(dto);
            }
        }

        public void Register(string userId)
        {
            lock (_lock)
            {
                foreach (var notification in _notifications)
                {
                    Clients.Client(Context.ConnectionId).onNotification(notification);
                }
            }
        }

        public class UserNotificationDto
        {
            public string Message { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
