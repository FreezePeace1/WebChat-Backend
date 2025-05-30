namespace SIPBackend.Domain.Entities;

public class WebChatRoom
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstParticipantId { get; set; } = string.Empty;
    public string SecondParticipantId { get; set; } = string.Empty;
}