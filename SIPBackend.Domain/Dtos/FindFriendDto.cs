using System.ComponentModel.DataAnnotations;

namespace SIPBackend.Domain.Dtos;

public class FindFriendDto
{
    [Required] 
    public string UserName { get; set; } = string.Empty;
}