using System.ComponentModel.DataAnnotations;

namespace SIPBackend.Domain.Dtos;

public record ChangeUserInfoDto([MinLength(3)]string? NewUserName,
    [EmailAddress]string? NewEmail,
    [Phone]string? NewPhoneNumber,
    string OldUserName,
    string OldEmail,
    string OldPhoneNumber);