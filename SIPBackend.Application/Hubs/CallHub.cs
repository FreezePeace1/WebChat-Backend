using Microsoft.AspNetCore.SignalR;
using SIPBackend.Domain.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SIPBackend.Application.Hubs;

public class CallHub : Hub
{
    private static Dictionary<string, List<string>> RoomConnections = new Dictionary<string, List<string>>();

    //Логгирование клиента
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"Client disconnected: {Context.ConnectionId}");

        //Remove the connection ID from the RoomConnections dictionary if it exists
        foreach (var room in RoomConnections.ToList()) // ToList() to avoid modification during iteration
        {
            if (room.Value.Contains(Context.ConnectionId))
            {
                room.Value.Remove(Context.ConnectionId);

                // If the room is empty, remove it
                if (room.Value.Count == 0)
                {
                    RoomConnections.Remove(room.Key);
                }
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        Console.WriteLine($"Client {Context.ConnectionId} joined room {roomId}");

        // Add the connection ID to the RoomConnections dictionary
        if (!RoomConnections.ContainsKey(roomId))
        {
            RoomConnections[roomId] = new List<string>();
        }
        RoomConnections[roomId].Add(Context.ConnectionId);

        // Get other users in the room
        var otherUsers = RoomConnections[roomId].Where(id => id != Context.ConnectionId).ToList();

        // Send offer to the new user if there are other users in the room
        if (otherUsers.Any())
        {
            Console.WriteLine($"[SERVER] Sending Offer request to existing users in room {roomId} for new user {Context.ConnectionId}");
             foreach (var existingClientId in otherUsers)
            {
              // Important: For each existing client, ask them to create and send offer to the new joining client.
              await Clients.Client(existingClientId).SendAsync("NeedsOffer", Context.ConnectionId);
            }
        }

        // Отправляем подтверждение на фронтенд
        await Clients.Caller.SendAsync("RoomJoined", roomId);
    }

        public async Task SendOfferToClient(string roomId, RTCSessionDescriptionInit offer, string targetClientId)
    {
        Console.WriteLine($"[SERVER] Sending Offer DIRECTLY to client {targetClientId} in room {roomId}");
        await Clients.Client(targetClientId).SendAsync("ReceiveOffer", offer);
    }

    public async Task SendOffer(string roomId, RTCSessionDescriptionInit offer)
    {
        Console.WriteLine($"[SERVER] Sending Offer to others in room {roomId}");
        await Clients.OthersInGroup(roomId).SendAsync("ReceiveOffer", offer);
    }

    public async Task SendAnswer(string roomId, RTCSessionDescriptionInit answer)
    {
        Console.WriteLine($"[SERVER] Sending Answer to others in room {roomId}");
        await Clients.OthersInGroup(roomId).SendAsync("ReceiveAnswer", answer);
    }

    public async Task SendIceCandidate(string roomId, RTCIceCandidateInit candidate)
    {
        if (string.IsNullOrEmpty(candidate.candidate) ||
            candidate.sdpMLineIndex == null)
        {
            Console.WriteLine("Invalid ICE candidate received");
            return;
        }

        await Clients.OthersInGroup(roomId).SendAsync("ReceiveIceCandidate", candidate);
    }
}