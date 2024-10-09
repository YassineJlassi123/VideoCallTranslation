using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

public class VideoCallHub : Hub
{
    private readonly CohereService _cohereService;

    // Inject the CohereService in the constructor
    public VideoCallHub(CohereService cohereService)
    {
        _cohereService = cohereService;
    }

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
        // Handle user disconnection logic if necessary
        await base.OnDisconnectedAsync(exception);
    }

    // Add the translation command function
    public async Task Command(string roomId, string text, string sourceLang, string targetLang)
    {
        // Validate the input
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(sourceLang) || string.IsNullOrWhiteSpace(targetLang))
        {
            await Clients.Caller.SendAsync("Error", "Invalid input for translation.");
            return;
        }

        // Prepare the prompt for the AI model
        var prompt = $@"
You are a language translation and correction expert. The user will provide input in one language, and you need to:
1. Correct any spelling or grammatical errors in the user's input.
2. Translate the corrected input from the user's source language to the target language.

Important:
- Do not include the original input in your response.
- Only return the final corrected and translated text.

User Input: '{text}'
Source Language: {sourceLang}
Target Language: {targetLang}";

        // Get the response from the AI service
        var response = await _cohereService.GetChatResponseAsync(prompt);

        // Send the translated text back to the room
        await Clients.Group(roomId).SendAsync("ReceiveTranslation", response.Trim(), sourceLang, targetLang);
    }
}
