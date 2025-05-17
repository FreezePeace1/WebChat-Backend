using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace SIPBackend.Domain.Dtos;

public record RegistrationDto(
    [Required] string UserName,
    [EmailAddress(ErrorMessage = "Введите адрес почты правильно"), Required]
    string Email,
    [Phone, Required] string PhoneNumber,
    [Required, DataType(DataType.Password)]
    string Password,
    [DataType(DataType.Password)] string RepeatPassword);