namespace SIPBackend.Domain.Models;

public class RefreshToken
{
    public required string Token { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Expired { get; set; }
}