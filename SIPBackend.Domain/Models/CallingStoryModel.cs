namespace SIPBackend.Domain.Models;

public class CallingStoryModel
{
    public int Id { get; set; }
    public string SecondParticipantName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
}