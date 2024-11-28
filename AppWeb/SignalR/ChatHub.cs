using Microsoft.AspNetCore.SignalR;

namespace AppWeb.SignalR
{
    public class ChatHub : Hub
    {
        public async Task SendMessageToCommunity(string message)
        {
            await Clients.All.SendAsync("ReceiveCommunityChat", message);
        }

    }
}
