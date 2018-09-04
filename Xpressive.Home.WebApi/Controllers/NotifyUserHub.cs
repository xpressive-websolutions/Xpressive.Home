using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using Xpressive.Home.Contracts.Messaging;

namespace Xpressive.Home.WebApi.Controllers
{
    [HubName("userNotifications")]
    public class NotifyUserHub : Hub, IMessageQueueListener<NotifyUserMessage>
    {
        void IMessageQueueListener<NotifyUserMessage>.Notify(NotifyUserMessage message)
        {
            Clients.All.broadcastMessage(message.Notification);
        }
    }
}
