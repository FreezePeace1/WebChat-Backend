using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using MimeKit.Text;
using Serilog;
using SIPBackend.Application.Interfaces;
using SIPBackend.Application.Resources;
using SIPBackend.DAL.Context;
using SIPBackend.DAL.Resources;
using SIPBackend.Domain;
using SIPBackend.Domain.Dtos;
using SIPBackend.Domain.Entities;
using SIPBackend.Domain.Models;

namespace SIPBackend.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IHttpContextAccessor _httpContext;
    private readonly SIPBackendContext _context;
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;

    public AuthService(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager,
        IHttpContextAccessor httpContext, SIPBackendContext context, ILogger logger,
        SignInManager<AppUser> signInManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _httpContext = httpContext;
        _context = context;
        _logger = logger;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    public async Task<ResponseDto> Registration(RegistrationDto dto)
    {
        try
        {
            if (dto.Password != dto.RepeatPassword)
            {
                return new ResponseDto()
                {
                    ErrorMessage = ErrorMessage.PasswordsAreNotEqual,
                    ErrorCode = (int)ErrorCodes.PasswordsAreNotEqual
                };
            }

            var existedUser = await _userManager.FindByEmailAsync(dto.Email);

            if (existedUser is not null)
            {
                return new ResponseDto()
                {
                    ErrorMessage = ErrorMessage.UserAlreadyExists,
                    ErrorCode = (int)ErrorCodes.UserAlreadyExists
                };
            }

            var user = new AppUser()
            {
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                UserName = dto.UserName
            };

            var registerResult = await _userManager.CreateAsync(user, dto.Password);

            if (!registerResult.Succeeded)
            {
                var errors = registerResult.Errors.Select(e => e.Description);

                return new ResponseDto()
                {
                    ErrorMessage = $"{ErrorMessage.ModelCreatingIsFailed} {errors}",
                    ErrorCode = (int)ErrorCodes.ModelCreatingIsFailed
                };
            }

            await _userManager.AddToRoleAsync(user, UserRoles.USER);

            var refreshToken = GenerateRefreshToken();
            SetRefreshToken(refreshToken);

            user.RefreshToken = refreshToken.Token;
            user.RefreshTokenCreated = refreshToken.Created;
            user.RefreshTokenExpires = refreshToken.Expired;

            await _context.SaveChangesAsync();

            var accessToken = await GenerateAndSetAccessToken(user);
            
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_configuration.GetSection("Gmail:EmailUsername").Value));
            email.To.Add(MailboxAddress.Parse(user.Email));
            email.Subject = "Message from ElectroStore";
            email.Body = new TextPart(TextFormat.Html) { Text = "Вы успешно зарегались на хуйне.com Ваши credentials\n" +
                                                                $"Логин: {dto.Email} Пароль: {dto.Password}" };

            using var smtp = new SmtpClient();
        
            await smtp.ConnectAsync(_configuration.GetSection("Gmail:EmailHost").Value, 587,SecureSocketOptions.StartTls);
        
            await smtp.AuthenticateAsync(_configuration.GetSection("Gmail:EmailUsername").Value, _configuration.GetSection("Gmail:EmailPassword").Value);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            return new ResponseDto()
            {
                SuccessMessage = SuccessMessage.MessageWasSentSuccessfully
            };

            return new ResponseDto()
            {
                SuccessMessage =
                    $"{CookieInfo.accessToken}: {accessToken} \n\n\n {CookieInfo.refreshToken}: {refreshToken.Token}"
            };
        }
        catch (Exception e)
        {
            _logger.Error(e, e.Message);

            return new ResponseDto()
            {
                ErrorMessage = ErrorMessage.FailedToRegisterUser,
                ErrorCode = (int)ErrorCodes.FailedToRegisterUser
            };
        }
    }

    private RefreshToken GenerateRefreshToken()
    {
        var refreshToken = new RefreshToken()
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expired = DateTime.UtcNow.AddDays(30),
            Created = DateTime.UtcNow
        };

        return refreshToken;
    }

    private void SetRefreshToken(RefreshToken refreshToken)
    {
        var cookieOpt = new CookieOptions()
        {
            HttpOnly = true,
            Expires = refreshToken.Expired
        };

        _httpContext.HttpContext.Response.Cookies.Append(CookieInfo.refreshToken, refreshToken.Token, cookieOpt);
    }

    private async Task<string> GenerateAndSetAccessToken(AppUser user)
    {
        var userRoles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("JWTID", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };

        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var accessToken = GenerateJWT(claims);
        SetAccessToken(accessToken);

        return accessToken;
    }

    private void SetAccessToken(string accessToken)
    {
        var cookieOpts = new CookieOptions()
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddMinutes(CookieInfo.AccessTokenExpiresTime)
        };

        _httpContext.HttpContext.Response.Cookies.Append(CookieInfo.accessToken, accessToken, cookieOpts);
    }

    private string GenerateJWT(List<Claim> claims)
    {
        var secret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]));

        var tokenObject = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(CookieInfo.AccessTokenExpiresTime),
            signingCredentials: new SigningCredentials(secret, SecurityAlgorithms.HmacSha256)
        );

        string token = new JwtSecurityTokenHandler().WriteToken(tokenObject);

        return token;
    }

    public async Task<ResponseDto<string>> SetAccessTokenForMiddleware(AppUser? user)
    {
        try
        {
            if (user != null)
            {
                var accessToken = await GenerateAndSetAccessToken(user);

                var refreshToken = GenerateRefreshToken();
                user.RefreshToken = refreshToken.Token;
                user.RefreshTokenCreated = refreshToken.Created;
                user.RefreshTokenExpires = refreshToken.Expired;
                await _context.SaveChangesAsync();

                SetRefreshToken(refreshToken);

                return new ResponseDto<string>()
                {
                    Data = accessToken,
                    SuccessMessage = SuccessMessage.RefreshTokenGotSuccessfully
                };
            }

            return new ResponseDto<string>()
            {
                Data = string.Empty,
                ErrorMessage = ErrorMessage.UserAreNotAuthorized,
                ErrorCode = (int)ErrorCodes.UserAreNotAuthorized
            };
        }
        catch (Exception e)
        {
            _logger.Error(e, e.Message);

            return new ResponseDto<string>()
            {
                ErrorMessage = ErrorMessage.FailedToGetRefreshToken,
                ErrorCode = (int)ErrorCodes.FailedToGetRefreshToken
            };
        }
    }

    public async Task<ResponseDto<AppUser>> GetUserByRefreshToken(string refreshToken)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.RefreshToken == refreshToken);

            if (user == null)
            {
                return new ResponseDto<AppUser>()
                {
                    ErrorMessage = ErrorMessage.UserNotExists,
                    ErrorCode = (int)ErrorCodes.UserNotExists
                };
            }

            return new ResponseDto<AppUser>()
            {
                Data = user
            };
        }
        catch (Exception e)
        {
            _logger.Error(e, e.Message);

            return new ResponseDto<AppUser>()
            {
                ErrorMessage = ErrorMessage.FailedToGetUserInfo,
                ErrorCode = (int)ErrorCodes.FailedToGetUserInfo
            };
        }
    }

    public async Task<ResponseDto> Login(LoginDto dto)
    {
        try
        {
            var existedUser = await _userManager.FindByEmailAsync(dto.Email);

            if (existedUser is null)
            {
                return new ResponseDto()
                {
                    ErrorMessage = ErrorMessage.UserNotExists,
                    ErrorCode = (int)ErrorCodes.UserNotExists
                };
            }

            if (!(await _userManager.CheckPasswordAsync(existedUser, dto.Password)))
            {
                return new ResponseDto()
                {
                    ErrorMessage = ErrorMessage.IncorrectCredentials,
                    ErrorCode = (int)ErrorCodes.IncorrectCredentials
                };
            }

            await _signInManager.PasswordSignInAsync(existedUser,
                dto.Password, true, true);

            var accessToken = await GenerateAndSetAccessToken(existedUser);


            var refreshToken = new RefreshToken()
            {
                Token = existedUser.RefreshToken,
                Expired = existedUser.RefreshTokenExpires,
                Created = existedUser.RefreshTokenCreated
            };

            if (existedUser.RefreshTokenExpires < DateTime.UtcNow)
            {
                existedUser.RefreshTokenExpires = DateTime.UtcNow.AddDays(30);
                refreshToken.Expired = existedUser.RefreshTokenExpires;

                await _context.SaveChangesAsync();
            }

            SetRefreshToken(refreshToken);

            return new ResponseDto()
            {
                SuccessMessage =
                    $"{CookieInfo.accessToken}: {accessToken} \n\n\n {CookieInfo.refreshToken}: {refreshToken.Token}"
            };
        }
        catch (Exception e)
        {
            _logger.Error(e, e.Message);

            return new ResponseDto()
            {
                ErrorMessage = ErrorMessage.FailedToLoginUser,
                ErrorCode = (int)ErrorCodes.FailedToLoginUser
            };
        }
    }

    public async Task<ResponseDto> Logout()
    {
        try
        {
            await _signInManager.SignOutAsync();
            _httpContext.HttpContext.Response.Cookies.Delete(CookieInfo.accessToken);
            _httpContext.HttpContext.Response.Cookies.Delete(CookieInfo.refreshToken);

            return new ResponseDto()
            {
                SuccessMessage = SuccessMessage.LogoutIsSucceed
            };
        }
        catch (Exception e)
        {
            _logger.Error(e, e.Message);

            return new ResponseDto()
            {
                ErrorMessage = ErrorMessage.FailedToLogout,
                ErrorCode = (int)ErrorCodes.FailedToLogout
            };
        }
    }

    public async Task<ResponseDto> ForgotPassword(string email)
    {
        try
        {
            var userByEmail = await _userManager.FindByEmailAsync(email);

            if (userByEmail is null)
            {
                return new ResponseDto()
                {
                    ErrorMessage = ErrorMessage.UserNotExists,
                    ErrorCode = (int)ErrorCodes.UserNotExists
                };
            }

            var resettingPasswordToken = CreateRandomToken();
            var message = $"Token for resetting password: \n {resettingPasswordToken}";
            await SendMessageWithToken(userByEmail, message);

            userByEmail.ResetPasswordToken = resettingPasswordToken;
            userByEmail.ResetPasswordTokenExpires = DateTime.Now.AddHours(12);

            await _context.SaveChangesAsync();

            return new ResponseDto()
            {
                SuccessMessage = SuccessMessage.SendingResettingPasswordTokenIsSuccessful
            };
        }
        catch (Exception e)
        {
            return new ResponseDto()
            {
                ErrorMessage = ErrorMessage.FailedToSendMessageWithResettingToken,
                ErrorCode = (int)ErrorCodes.FailedToSendMessage
            };
        }
    }

    private string CreateRandomToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
    }

    private async Task<ResponseDto> SendMessageWithToken(AppUser user, string message)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_configuration.GetSection("Gmail:EmailUsername").Value));
            email.To.Add(MailboxAddress.Parse(user.Email));
            email.Subject = "Email for resetting password";
            email.Body = new TextPart(TextFormat.Html) { Text = message };

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(_configuration.GetSection("EmailHost").Value, 587, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_configuration["Gmail:EmailUsername"], _configuration["Gmail:EmailPassword"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            return new ResponseDto()
            {
                SuccessMessage = SuccessMessage.MessageWasSentSuccessfully
            };
        }
        catch (Exception e)
        {
            _logger.Error(e, e.Message);

            return new ResponseDto()
            {
                ErrorMessage = ErrorMessage.FailedToSendMessage,
                ErrorCode = (int)ErrorCodes.FailedToSendMessage
            };
        }
    }

    public async Task<ResponseDto> ResetPassword(ResetPasswordDto dto)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(x =>
                x.ResetPasswordToken == dto.ResettingPasswordToken);

            if (user is null || user.ResetPasswordTokenExpires < DateTime.UtcNow)
            {
                return new ResponseDto()
                {
                    ErrorMessage = ErrorMessage.ResettingPasswordTokenIsInvalid,
                    ErrorCode = (int)ErrorCodes.ResettingPasswordTokenIsInvalid
                };
            }

            var passwordHasher = new PasswordHasher<AppUser>();
            user.PasswordHash = passwordHasher.HashPassword(user, dto.NewPassword);
            user.ResetPasswordToken = null;
            user.ResetPasswordTokenExpires = null;

            await _context.SaveChangesAsync();

            return new ResponseDto()
            {
                SuccessMessage = SuccessMessage.ResettingPasswordTokenIsActivated
            };
        }
        catch (Exception e)
        {
            _logger.Error(e, e.Message);

            return new ResponseDto()
            {
                ErrorMessage = ErrorMessage.FailedToResetPassword,
                ErrorCode = (int)ErrorCodes.FailedToResetPassword
            };
        }
    }

    public async Task<ResponseDto> ChangeInfo(ChangeUserInfoDto dto)
    {
        try
        {
            var userName = _httpContext.HttpContext.User.Identity.Name;

            if (userName == null)
            {
                return new ResponseDto<AppUser>()
                {
                    ErrorMessage = ErrorMessage.UserAreNotAuthorized,
                    ErrorCode = (int)ErrorCodes.UserAreNotAuthorized
                };
            }

            var user = await _userManager.FindByNameAsync(userName);

            if (user is null)
            {
                return new ResponseDto<AppUser>()
                {
                    ErrorMessage = ErrorMessage.UserAreNotAuthorized,
                    ErrorCode = (int)ErrorCodes.UserAreNotAuthorized
                };
            }

            if (dto.NewEmail != null && await _userManager.FindByEmailAsync(dto.NewEmail) is null)
            {
                user.Email = dto.NewEmail;
            }

            if (dto.NewUserName != null && await _userManager.FindByNameAsync(dto.NewUserName) is null)
            {
                user.UserName = dto.NewUserName ?? user.UserName;
            }

            user.PhoneNumber = dto.NewPhoneNumber ?? user.PhoneNumber;

            await _context.SaveChangesAsync();

            return new ResponseDto()
            {
                SuccessMessage = SuccessMessage.UserInfoIsChangedSuccessfully
            };
        }
        catch (Exception e)
        {
            _logger.Error(e, e.Message);

            return new ResponseDto()
            {
                ErrorMessage = ErrorMessage.FailedToChangeUserInfo,
                ErrorCode = (int)ErrorCodes.FailedToChangeUserInfo
            };
        }
    }

    public async Task<ResponseDto<AppUser>> Account()
    {
        try
        {
            var userName = _httpContext.HttpContext.User.Identity.Name;

            if (userName == null)
            {
                return new ResponseDto<AppUser>()
                {
                    ErrorMessage = ErrorMessage.UserAreNotAuthorized,
                    ErrorCode = (int)ErrorCodes.UserAreNotAuthorized
                };
            }

            var user = await _userManager.FindByNameAsync(userName);

            if (user is null)
            {
                return new ResponseDto<AppUser>()
                {
                    ErrorMessage = ErrorMessage.UserAreNotAuthorized,
                    ErrorCode = (int)ErrorCodes.UserAreNotAuthorized
                };
            }

            return new ResponseDto<AppUser>()
            {
                Data = user,
                SuccessMessage = SuccessMessage.AccountHasReachedSuccessfully
            };
        }
        catch (Exception e)
        {
            _logger.Error(e, e.Message);

            return new ResponseDto<AppUser>()
            {
                ErrorMessage = ErrorMessage.FailedToGetAccountInfo,
                ErrorCode = (int)ErrorCodes.FailedToGetAccountInfo
            };
        }
    }
}