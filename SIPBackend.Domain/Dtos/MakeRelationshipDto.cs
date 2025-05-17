using System.ComponentModel.DataAnnotations;

namespace SIPBackend.Domain.Dtos;

public class MakeRelationshipDto
{
    [Required] 
    public string UserName { get; set; } = string.Empty;
}