using SIPBackend.Domain.Entities;

namespace SIPBackend.Domain.Models;

public class ChatInfo
{
    public string ChatId { get; set; } = string.Empty;
    public List<string> ParticipantsUserNames { get; set; }
}