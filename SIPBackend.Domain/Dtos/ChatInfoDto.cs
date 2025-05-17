using SIPBackend.Domain.Entities;

namespace SIPBackend.Domain.Dtos;

public class ChatInfoDto
{
    public string ConsumerUserName { get; set; }
    public string SenderUserName { get; set; }
    public List<Message> Messages { get; set; }
}