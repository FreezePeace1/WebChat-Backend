using System.ComponentModel.DataAnnotations;

namespace SIPBackend.Domain.Entities;

public class Chat
{
    [Key]
    public Guid ChatId { get; set; }
    
    public ChatParticipants ChatParticipants { get; set; }
    public int ChatParticipantsId { get; set; }
    
    public List<Message> Messages { get; set; }
}