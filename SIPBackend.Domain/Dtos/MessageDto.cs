namespace SIPBackend.Domain.Dtos;

public class MessageDto
{
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public bool IsCurrentUser { get; set; }
}