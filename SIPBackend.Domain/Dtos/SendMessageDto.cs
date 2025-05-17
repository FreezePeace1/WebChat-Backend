namespace SIPBackend.Domain.Dtos;

public class SendMessageDto
{
    public Guid ChatId { get; set; }
    public string Content { get; set; }
}