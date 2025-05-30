using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SIPBackend.Application.Interfaces;
using SIPBackend.Application.Resources;
using SIPBackend.DAL.Context;
using SIPBackend.DAL.Resources;
using SIPBackend.Domain;
using SIPBackend.Domain.Dtos;
using SIPBackend.Domain.Entities;

namespace SIPBackend.Application.Services;

public sealed class CallingStoryService : ICallingStoryService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SIPBackendContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ILogger _logger;
    
    public CallingStoryService(UserManager<AppUser> userManager, SIPBackendContext dbContext, IHttpContextAccessor httpContext, ILogger logger)
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
    
    public async Task<ResponseDto<IReadOnlyList<CallingStory>>> GetUserCallingStoriesAsync(CancellationToken ct)
    {
        try
        {
            var userInCookie = await IsUserExistsInCookieAsync();

            if (!userInCookie.IsSucceed)
            {
                return new ResponseDto<IReadOnlyList<CallingStory>>()
                {
                    ErrorCode = userInCookie.ErrorCode,
                    ErrorMessage = userInCookie.ErrorMessage,
                };
            }

            var callingStories = await _dbContext.CallingStories
                .Where(x => x.FirstParticipantId == userInCookie.Data.Id
                            || x.SecondParticipantId == userInCookie.Data.Id).ToListAsync(cancellationToken: ct);

            return new ResponseDto<IReadOnlyList<CallingStory>>()
            {
                Data = callingStories,
                SuccessMessage = SuccessMessage.GettingCallingStoryIsSuccessfull
            };

        }
        catch(OperationCanceledException e)
        {
            _logger.Error(e, e.Message);
            
            throw;
        }
        catch (Exception e)
        {
           _logger.Error(e,e.Message);

           return new ResponseDto<IReadOnlyList<CallingStory>>()
           {
                ErrorMessage = ErrorMessage.GettingCallingStoryIsFailed,
                ErrorCode = (int)ErrorCodes.GettingCallingStoryIsFailed
           };
        }
    }

    public async Task<ResponseDto> StartUsersCallingStoryAsync(CallingStoryDto dto)
    {
        try
        {
            var user = await IsUserExistsInCookieAsync();

            if (!user.IsSucceed)
            {
                return new ResponseDto()
                {
                    ErrorCode = user.ErrorCode,
                    ErrorMessage = user.ErrorMessage
                };
            }

            var newCallingStory = new CallingStory()
            {
                FirstParticipantId = user.Data.Id,
                SecondParticipantId = dto.SecondParticipantId,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.CallingStories.AddAsync(newCallingStory);
            await _dbContext.SaveChangesAsync();

            return new ResponseDto()
            {
                SuccessMessage = SuccessMessage.NewCallingStoryIsAddedSuccessfully
            };
        }
        catch (Exception e)
        {
           _logger.Error(e, e.Message);

           return new ResponseDto()
           {
                ErrorMessage = ErrorMessage.FailedToAddNewCallingStory
           };
        }
    }

    public async Task<ResponseDto> EndUsersCallingStoryAsync(CallingStoryDto dto)
    {
        try
        {
            var user = await IsUserExistsInCookieAsync();

            if (!user.IsSucceed)
            {
                return new ResponseDto()
                {
                    ErrorCode = user.ErrorCode,
                    ErrorMessage = user.ErrorMessage
                };
            }
            
            var existedNotEndedCallingStory = await _dbContext.CallingStories
                .FirstOrDefaultAsync(x => (x.FirstParticipantId == user.Data.Id && x.SecondParticipantId == dto.SecondParticipantId
                               || x.FirstParticipantId == dto.SecondParticipantId && x.SecondParticipantId == user.Data.Id) && x.EndedAt == null);

            if (existedNotEndedCallingStory is not null)
            {
                existedNotEndedCallingStory.EndedAt = DateTime.UtcNow;
                
                await _dbContext.SaveChangesAsync();

                return new ResponseDto()
                {
                    SuccessMessage = SuccessMessage.CallIsEndedSuccessfully
                };
            }

            return new ResponseDto()
            {
                ErrorMessage = ErrorMessage.FailedToFindCallingStory,
                ErrorCode = (int)ErrorCodes.FailedToFindCallingStory
            };
        }
        catch (Exception e)
        {
           _logger.Error(e, e.Message);

           return new ResponseDto()
           {
                ErrorMessage = ErrorMessage.FailedToEndCallingStory,
                ErrorCode = (int)ErrorCodes.FailedToEndCallingStory
           };
        }
    }
}