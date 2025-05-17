using System.ComponentModel.DataAnnotations;

namespace SIPBackend.Domain.Dtos;

public class ForgotPasswordDto
{
    [Required, EmailAddress] 
    public string Email { get; set; } = string.Empty;
}