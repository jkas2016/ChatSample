using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatSample.Hubs;

[Authorize]
public class ChatHub(ILogger<ChatHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        
        // ReceiveMessage 를 구독한 클라이언트에게 해당 유저가 접속했음을 알립니다.
        var user = Context.User?.Identity?.Name ?? "Anonymous";
        await Clients.All.SendAsync("ReceiveMessage", "System", $"{user} has joined the chat");
        
        logger.LogInformation("OnConnectedAsync: {user} {ConnectionId}", user, Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        
        // ReceiveMessage 를 구독한 클라이언트에게 해당 유저가 접속을 끊었음을 알립니다.
        var user = Context.User?.Identity?.Name ?? "Anonymous";
        await Clients.All.SendAsync("ReceiveMessage", "System", $"{user} has left the chat");
        
        logger.LogInformation("OnDisconnectedAsync: {user} {ConnectionId}", user, Context.ConnectionId);
    }

    public async Task SendMessage(string state, string message)
    {
        var user = Context.User?.Identity?.Name ?? "Anonymous";
        await Clients.All.SendAsync("ReceiveMessage", user, state, message);
        
        logger.LogInformation("SendMessage: {user} {state} {message}", user, state, message);
    }
}