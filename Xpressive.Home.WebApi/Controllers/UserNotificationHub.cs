using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.WebApi.Controllers
{
    [HubName("notificationHub")]
    public class UserNotificationHub : Hub, IMessageQueueListener<NotifyUserMessage>
    {
        private static readonly ConcurrentDictionary<string, string> _connectionsByUser = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, HashSet<string>> _sentNotifications = new ConcurrentDictionary<string, HashSet<string>>();
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

                _notifications.Add(dto);

                var connectionIds = _connectionsByUser.Keys.ToList();
                Clients.Clients(connectionIds).onNotification(dto);
            }
        }

        public void Register(string userId)
        {
            lock (_lock)
            {
                _connectionsByUser.TryAdd(Context.ConnectionId, userId);
                var alreadySent = _sentNotifications.GetOrAdd(userId, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                var toSend = _notifications.Where(n => !alreadySent.Contains(n.Id));

                foreach (var notification in toSend)
                {
                    Clients.Client(Context.ConnectionId).onNotification(notification);
                }
            }
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            lock (_lock)
            {
                string userId;
                if (_connectionsByUser.TryRemove(Context.ConnectionId, out userId))
                {
                    HashSet<string> notificationIds;
                    _sentNotifications.TryRemove(userId, out notificationIds);
                }
                return base.OnDisconnected(stopCalled);
            }
        }

        public class UserNotificationDto
        {
            public string Id { get; set; }
            public string Message { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
