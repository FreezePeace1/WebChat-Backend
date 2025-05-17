namespace SIPBackend.Application.Hubs;

public interface IChatClient
{
    public Task ReceiveMessage(string user, string message);
}