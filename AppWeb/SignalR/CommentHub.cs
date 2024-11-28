using DbContext.Models;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppWeb.SignalR
{
    public class CommentHub : Hub
    {
        public async Task SendComment(string postId, string comment)
        {
            await Clients.All.SendAsync("ReceiveComment", postId, comment);
        }
        public async Task DeleteComment(string postId, string commentId)
        {
            await Clients.All.SendAsync("RecDelComment", postId, commentId);
        }
    }
}
