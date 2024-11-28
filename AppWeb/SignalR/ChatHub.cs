using DbContext.Models;
using Microsoft.AspNetCore.SignalR;

namespace AppWeb.SignalR
{
    public class ChatHub : Hub
    {
        public async Task SendMenssageToCommunity(string message)
        {
            await Clients.All.SendAsync("RecieveCommunityChat", message);
        }
        public async Task SendMenssage(string user, string userToRecieved, string message)
        {
        }
    }
}
