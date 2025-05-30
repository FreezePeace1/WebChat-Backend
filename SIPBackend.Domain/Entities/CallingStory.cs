namespace SIPBackend.Domain.Entities;

public class CallingStory
{
    public int Id { get; set; }
    public string FirstParticipantId { get; set; } = string.Empty;
    public string SecondParticipantId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
}