using Microsoft.AspNetCore.Identity;

namespace SIPBackend.Domain.Entities;

public sealed class AppUser : IdentityUser
{
    public string? ResetPasswordToken { get; set; }
    public DateTime? ResetPasswordTokenExpires { get; set; } 
    public string RefreshToken { get; set; } = string.Empty;
    
    public DateTime RefreshTokenCreated { get; set; } = DateTime.UtcNow;

    public DateTime RefreshTokenExpires { get; set; } = DateTime.UtcNow;
}