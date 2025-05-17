using SIPBackend.Domain.Dtos;
using SIPBackend.Domain.Entities;

namespace SIPBackend.Application.Interfaces;

public interface IAuthService
{
    /*
     * REGISTRATION LOGIN LOGOUT RESETPASSWORD CHANGEINFO ACCOUNT (USING COOKIE)
     */

    Task<ResponseDto> Registration(RegistrationDto dto);
    Task<ResponseDto> Login(LoginDto dto);
    Task<ResponseDto> Logout();
    Task<ResponseDto> ForgotPassword(string email);
    Task<ResponseDto> ResetPassword(ResetPasswordDto dto);
    Task<ResponseDto> ChangeInfo(ChangeUserInfoDto dto);
    Task<ResponseDto<AppUser>> Account();
    Task<ResponseDto<string>> SetAccessTokenForMiddleware(AppUser? user);
    Task<ResponseDto<AppUser>> GetUserByRefreshToken(string refreshToken);
}