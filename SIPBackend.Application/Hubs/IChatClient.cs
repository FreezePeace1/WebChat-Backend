using SIPBackend.Domain.Dtos;

namespace SIPBackend.Application.Hubs;

public interface IChatClient
{
    public Task ReceiveMessage(string user, string message);
    Task LoadMessageHistory(List<MessageDto> messages);
}