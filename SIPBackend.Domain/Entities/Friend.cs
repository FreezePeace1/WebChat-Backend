using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SIPBackend.Domain.Entities;

public sealed class Friend
{
    [Key]
    public int Id { get; set; }
    [Column(TypeName = "text")] 
    public string UserId1 { get; set; }
    [Column(TypeName = "text")] 
    public string UserId2 { get; set; }

    public bool IsAccepted { get; set; } = false;
}