using System.ComponentModel.DataAnnotations;

namespace SIPBackend.Domain.Dtos;

public record LoginDto (
    [Required] string Email,
    [Required,DataType(DataType.Password)] string Password
    );