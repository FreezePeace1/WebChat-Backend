using System.ComponentModel.DataAnnotations;

namespace SIPBackend.Domain.Dtos;

public class FriendWithIdDto
{
    [Required] 
    public string FriendId { get; set; } = string.Empty;
}