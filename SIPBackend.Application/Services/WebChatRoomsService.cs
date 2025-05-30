using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Core;
using SIPBackend.Application.Interfaces;
using SIPBackend.Application.Resources;
using SIPBackend.DAL.Context;
using SIPBackend.DAL.Resources;
using SIPBackend.Domain;
using SIPBackend.Domain.Dtos;
using SIPBackend.Domain.Entities;

namespace SIPBackend.Application.Services;

public sealed class WebChatRoomsService : IWebChatRoomsService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SIPBackendContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ILogger _logger;
    
    public WebChatRoomsService(UserManager<AppUser> userManager, SIPBackendContext dbContext, IHttpContextAccessor httpContext, ILogger logger)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _httpContext = httpContext;
        _logger = logger;
    }
    
    private async Task<ResponseDto<AppUser>> IsUserExistsInCookieAsync()
    {
        try
        {
            var userName = _httpContext.HttpContext.User.Identity.Name;

            if (string.IsNullOrEmpty(userName))
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
                    ErrorCode = (int)ErrorCodes.UserNotExists,
                    ErrorMessage = ErrorMessage.UserNotExists
                };
            }

            return new ResponseDto<AppUser>()
            {
                Data = user
            };
        }
        catch (Exception e)
        {
            _logger.Error(e,e.Message);

            return new ResponseDto<AppUser>()
            {
                ErrorMessage = ErrorMessage.FailedToGetUserInfo,
                ErrorCode = (int)ErrorCodes.FailedToGetUserInfo
            };
        }
    }
    
    public async Task<ResponseDto<WebChatRoom>> GetUsersWebChatRoomAsync(GetUsersWebChatRoomDto dto)
    {
        try
        {
            var userInCookie = await IsUserExistsInCookieAsync();

            if (!userInCookie.IsSucceed)
            {
                return new ResponseDto<WebChatRoom>()
                {
                    ErrorCode = userInCookie.ErrorCode,
                    ErrorMessage = userInCookie.ErrorMessage,
                };
            }
            
            var existedUsersWebChatRoom = await _dbContext.WebChatRooms
                .FirstOrDefaultAsync(x => x.FirstParticipantId == userInCookie.Data.Id && x.SecondParticipantId == dto.SecondParticipantId
                || x.SecondParticipantId == userInCookie.Data.Id && x.FirstParticipantId == dto.SecondParticipantId);

            if (existedUsersWebChatRoom is null)
            {
                var newChatRoom = await CreateNewChatRoomAsync(userInCookie.Data,dto);
                
                if (newChatRoom.IsSucceed)
                {
                    return new ResponseDto<WebChatRoom>()
                    {
                        Data = newChatRoom.Data,
                        SuccessMessage = newChatRoom.SuccessMessage
                    };   
                }

                return new ResponseDto<WebChatRoom>()
                {
                    ErrorMessage = newChatRoom.ErrorMessage,
                    ErrorCode = newChatRoom.ErrorCode
                };
            }

            return new ResponseDto<WebChatRoom>()
            {
                Data = existedUsersWebChatRoom,
                SuccessMessage = SuccessMessage.GettingUsersWebChatRoomIsSuccessfull
            };
        }
        catch (Exception e)
        {
            _logger.Error(e,e.Message);

            return new ResponseDto<WebChatRoom>()
            {
                ErrorMessage = ErrorMessage.GettingUsersWebChatRoomIsFailed,
                ErrorCode = (int)ErrorCodes.GettingUsersWebChatRoomIsFailed
            };
        }
    }

    private async Task<ResponseDto<WebChatRoom>> CreateNewChatRoomAsync(AppUser user,GetUsersWebChatRoomDto dto)
    {
        try
        {
            var newChatRoom = new WebChatRoom()
            {
                Id = Guid.NewGuid(),
                FirstParticipantId = user.Id,
                SecondParticipantId = dto.SecondParticipantId,
            };
            
            await _dbContext.WebChatRooms.AddAsync(newChatRoom);
            await _dbContext.SaveChangesAsync();

            return new ResponseDto<WebChatRoom>()
            {
                Data = newChatRoom,
                SuccessMessage = SuccessMessage.NewChatRoomIsCreatedSuccessfully
            };
        }
        catch (Exception e)
        {
            _logger.Error(e,e.Message);

            return new ResponseDto<WebChatRoom>()
            {
                ErrorMessage = ErrorMessage.FailedToCreateNewChatRoom,
                ErrorCode = (int)ErrorCodes.FailedToCreateNewChatRoom
            };
        }
    }
}