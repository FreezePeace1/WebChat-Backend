using System.ComponentModel.DataAnnotations;

namespace SIPBackend.Domain.Dtos;

public class ResetPasswordDto
{
    [Required(ErrorMessage = "Нужен токен для сброса пароля")]
    public string ResettingPasswordToken { get; set; }
    
    [Required(ErrorMessage = "Введите пароль"),DataType(DataType.Password),MinLength(8)]
    public string NewPassword { get; set; }
    
    [Required(ErrorMessage = "Проверьте правильность пароля"),Compare(nameof(NewPassword))]
    public string NewRepeatedPassword { get; set; }
}