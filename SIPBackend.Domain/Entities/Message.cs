using System.ComponentModel.DataAnnotations;

namespace SIPBackend.Domain.Entities;

public class Message
{
    [Key]
    public Guid MessageId { get; set; }  
    
    public Chat Chat { get; set; }
    public Guid ChatId { get; set; }
    
    public string UserSenderId { get; set; }
    public string UserConsumerId { get; set; }
    
    public string Content { get; set; }
    public DateTime SentAt { get; set; } = DateTime.Now;
}