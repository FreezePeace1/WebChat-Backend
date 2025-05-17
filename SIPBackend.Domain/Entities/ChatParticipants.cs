using System.ComponentModel.DataAnnotations;

namespace SIPBackend.Domain.Entities;

public class ChatParticipants
{
    [Key] public int ChatParticipantsId { get; set; }
    public string FirstUserId { get; set; }
    public string SecondUserId { get; set; }

    public Chat Chat { get; set; }
}