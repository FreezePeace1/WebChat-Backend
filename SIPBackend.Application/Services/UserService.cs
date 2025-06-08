using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Serilog;
using SIPBackend.Application.Interfaces;
using SIPBackend.Application.Resources;
using SIPBackend.DAL.Resources;
using SIPBackend.Domain;
using SIPBackend.Domain.Dtos;
using SIPBackend.Domain.Entities;

namespace SIPBackend.Application.Services;

public class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger _logger;
    private readonly IHttpContextAccessor _httpContext;
    
    public UserService(UserManager<AppUser> userManager, ILogger logger, IHttpContextAccessor httpContext)
    {
        _userManager = userManager;
        _logger = logger;
        _httpContext = httpContext;
    }
    
    public async Task<ResponseDto<SipCredentialsModel>> SipCredentials()
    {
        try
        {
            var userName = _httpContext.HttpContext.User.Identity.Name;

            if (string.IsNullOrEmpty(userName))
            {
                return new ResponseDto<SipCredentialsModel>()
                {
                    ErrorMessage = ErrorMessage.UserAreNotAuthorized,
                    ErrorCode = (int)ErrorCodes.UserAreNotAuthorized
                };
            }
            
            var user = await _userManager.FindByNameAsync(userName);

            if (user is null)
            {
                return new ResponseDto<SipCredentialsModel>()
                {
                    ErrorMessage = ErrorMessage.UserNotExists,
                    ErrorCode = (int)ErrorCodes.UserNotExists,
                };
            }

            var newModel = new SipCredentialsModel()
            {
                SipDomain = "192.168.1.100",
                SipPassword = user.PasswordHash,
                SipUsername = user.UserName
            };

            return new ResponseDto<SipCredentialsModel>()
            {
                Data = newModel,
                SuccessMessage = SuccessMessage.GettingAllUsersWithRequests 
            };

        }
        catch (Exception e)
        {
            _logger.Error(e, "SipCredentials error");
            throw;
        }
    }
}