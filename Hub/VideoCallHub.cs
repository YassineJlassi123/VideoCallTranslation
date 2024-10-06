using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

public class VideoCallHub : Hub
{
    public async Task SendOffer(string roomId, string offer)
    {
        await Clients.OthersInGroup(roomId).SendAsync("ReceiveOffer", offer);
    }

    public async Task SendAnswer(string roomId, string answer)
    {
        await Clients.OthersInGroup(roomId).SendAsync("ReceiveAnswer", answer);
    }

    public async Task SendIceCandidate(string roomId, string candidate)
    {
        await Clients.OthersInGroup(roomId).SendAsync("ReceiveIceCandidate", candidate);
    }

    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await Clients.OthersInGroup(roomId).SendAsync("UserJoined", Context.ConnectionId);
    }

    public async Task LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        await Clients.OthersInGroup(roomId).SendAsync("UserLeft", Context.ConnectionId);
    }

    public async Task SendTranslation(string roomId, string translation, string sourceLang, string targetLang)
    {
        await Clients.OthersInGroup(roomId).SendAsync("ReceiveTranslation", translation, sourceLang, targetLang);
    }

    public async Task EndCall(string roomId)
    {
        await Clients.Group(roomId).SendAsync("CallEnded");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        // You might want to implement logic here to notify other users when someone disconnects
        await base.OnDisconnectedAsync(exception);
    }
}