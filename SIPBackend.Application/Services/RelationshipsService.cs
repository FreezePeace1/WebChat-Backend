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

public class RelationshipsService : IRelationshipsService
{
    private readonly IHttpContextAccessor _httpContext;
    private readonly SIPBackendContext _dbContext;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger _logger;

    public RelationshipsService(IHttpContextAccessor httpContext, SIPBackendContext dbContext,
        UserManager<AppUser> userManager, ILogger logger)
    {
        _httpContext = httpContext;
        _dbContext = dbContext;
        _userManager = userManager;
        _logger = logger;
    }

    private async Task<ResponseDto<AppUser>> CheckAndGetUser()
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

        var hostedUser = await _userManager.FindByNameAsync(userName);
        if (hostedUser is null)
        {
            return new ResponseDto<AppUser>()
            {
                ErrorMessage = ErrorMessage.UserNotExists,
                ErrorCode = (int)ErrorCodes.UserNotExists
            };
        }

        return new ResponseDto<AppUser>()
        {
            Data = hostedUser
        };
    }
    
    public async Task<ResponseDto> MakeRelationship(MakeRelationshipDto dto)
    {
        try
        {
            var hostedUser = await CheckAndGetUser();
            var newFriend = await _userManager.FindByNameAsync(dto.UserName);
            if (newFriend is null)
            {
                return new ResponseDto()
                {
                    ErrorMessage = ErrorMessage.UserNotExists,
                    ErrorCode = (int)ErrorCodes.UserNotExists
                };
            }

            if (await IsFriends(hostedUser.Data.Id,newFriend.Id))
            {
                return new ResponseDto()
                {
                    ErrorMessage = ErrorMessage.UsersAreAlreadyFriends,
                    ErrorCode = (int)ErrorCodes.UsersAreAlreadyFriends
                };
            }

            var newFriends = await _dbContext.Friends.AddAsync(new Friend()
            {
                UserId1 = hostedUser.Data.Id,
                UserId2 = newFriend.Id,
                IsAccepted = false
            });

            await _dbContext.SaveChangesAsync();

            return new ResponseDto()
            {
                SuccessMessage = SuccessMessage.FriendAddedSuccessfully
            };
        }
        catch (Exception e)
        {
            _logger.Error(e, e.Message);

            return new ResponseDto()
            {
                ErrorMessage = ErrorMessage.FailedToAddNewFriend,
                ErrorCode = (int)ErrorCodes.FailedToAddNewFriend
            };
        }
    }

    private async Task<bool> IsFriends(string hostedUserId,string newFriendId)
    {
        return await _dbContext.Friends.AnyAsync(x =>
            x.UserId2 == newFriendId && x.UserId1 == hostedUserId
            || x.UserId1 == newFriendId && x.UserId2 == hostedUserId);
    }

    public async Task<ResponseDto<IReadOnlyList<FriendDto>>> GetAllRelationships()
    {
        try
        {
            var user = await CheckAndGetUser();
            
            var friendIds = await _dbContext.Friends.Where(f => (f.UserId1 == user.Data.Id || f.UserId2 == user.Data.Id) && f.IsAccepted)
                .Select(f => f.UserId1 == user.Data.Id ? f.UserId2 : f.UserId1) 
                .Distinct() 
                .ToListAsync();
            
            var friends = await _dbContext.Users
                .Where(u => friendIds.Contains(u.Id))
                .Select(u => new FriendDto
                {
                    Id = u.Id,
                    UserName = u.UserName
                })
                .AsNoTracking()
                .ToListAsync();
            
            return new ResponseDto<IReadOnlyList<FriendDto>>()
            {
                Data = friends,
                SuccessMessage = SuccessMessage.GettingFriendsIsSuccessful
            };
        }
        catch (Exception e)
        {
            _logger.Error(e, e.Message);

            return new ResponseDto<IReadOnlyList<FriendDto>>()
            {
                ErrorMessage = ErrorMessage.FailedToGetFriends,
                ErrorCode = (int)ErrorCodes.FailedToGetFriends
            };
        }
    }

    public async Task<ResponseDto> DeleteFriend(FriendWithIdDto withIdDto)
    {
        try
        {
            var friend = await _dbContext.Friends.FirstOrDefaultAsync(x => x.UserId2 == withIdDto.FriendId || x.UserId1 == withIdDto.FriendId);
            if (friend is null)
            {
                return new ResponseDto()
                {
                    ErrorMessage = ErrorMessage.UserNotExists,
                    ErrorCode = (int)ErrorCodes.UserNotExists
                };
            }

            _dbContext.Friends.Remove(friend);
            await _dbContext.SaveChangesAsync();

            return new ResponseDto()
            {
                SuccessMessage = SuccessMessage.DeletingFriendIsSuccessful
            };
        }
        catch (Exception e)
        {
           _logger.Error(e,e.Message);

           return new ResponseDto()
           {
                ErrorMessage = ErrorMessage.FailedToDeleteFriend,
                ErrorCode = (int)ErrorCodes.FailedToDeleteFriend
           };
        }
    }

    public async Task<ResponseDto<FriendInfo>> FindFriend(FindFriendDto dto)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(dto.UserName);
            
            if(user is null)
            {
                return new ResponseDto<FriendInfo>()
                {
                    ErrorMessage = ErrorMessage.UserNotExists,
                    ErrorCode = (int)ErrorCodes.UserNotExists
                };
            }
            
            return new ResponseDto<FriendInfo>()
            {
                Data = new FriendInfo(user.UserName,user.Id),
                SuccessMessage = SuccessMessage.FindingFriendIsSuccessful
            };
        }
        catch (Exception e)
        {
            _logger.Error(e,e.Message);

            return new ResponseDto<FriendInfo>()
            {
                ErrorMessage = ErrorMessage.FailedToFindFriend,
                ErrorCode = (int)ErrorCodes.FailedToFindFriend
            };
        }
    }

    public async Task<ResponseDto> AcceptFriendRequest(FriendWithIdDto withIdDto)
    {
        try
        {
            var user = await CheckAndGetUser();

            if (!(await IsFriends(user.Data.Id, withIdDto.FriendId)))
            {
                return new ResponseDto()
                {
                    ErrorMessage = ErrorMessage.UsersAreNotFriends,
                    ErrorCode = (int)ErrorCodes.UsersAreNotFriends
                };
            }

            var friends = await _dbContext.Friends.FirstOrDefaultAsync(x =>
                x.UserId2 == user.Data.Id && x.UserId1 == withIdDto.FriendId);

            if (friends is not null)
            {
                friends.IsAccepted = true;

                await _dbContext.SaveChangesAsync();

                return new ResponseDto()
                {
                    SuccessMessage = SuccessMessage.FriendsRequestApproved
                };
            }

            return new ResponseDto()
            {
                ErrorMessage = ErrorMessage.FailedToAcceptRequest,
                ErrorCode = (int)ErrorCodes.FailedToAcceptRequest
            };

        }
        catch (Exception e)
        {
            _logger.Error(e,e.Message);
            
            return new ResponseDto()
            {
                ErrorMessage = $"{ErrorMessage.FailedToAcceptRequest} {e.Message}",
                ErrorCode = (int)ErrorCodes.FailedToAcceptRequest
            };
        }
    }

    public async Task<ResponseDto<IReadOnlyList<FriendDto>>> GetAllRequests()
    {
        try
        {
            var user = await CheckAndGetUser();

            var allUserIdsWithRequests = await _dbContext.Friends
                .Where(x => x.UserId2 == user.Data.Id && x.IsAccepted == false)
                .Select(x => x.UserId1 == user.Data.Id ? x.UserId2 : x.UserId1)
                .Distinct()
                .ToListAsync();

            var neededUsersWithRequests = await _dbContext.Users.Where(x => allUserIdsWithRequests.Contains(x.Id))
                .Select(x => new FriendDto()
                {
                    Id = x.Id,
                    UserName = x.UserName
                })
                .AsNoTracking()
                .ToListAsync();
            
            return new ResponseDto<IReadOnlyList<FriendDto>>()
            {
                Data = neededUsersWithRequests,
                SuccessMessage = SuccessMessage.GettingAllUsersWithRequests
            };
        }
        catch (Exception e)
        {
            _logger.Error(e,e.Message);

            return new ResponseDto<IReadOnlyList<FriendDto>>()
            {
                ErrorMessage = ErrorMessage.FailedToGetAllUsersWithRequests,
                ErrorCode = (int)ErrorCodes.FailedToGetAllUsersWithRequests
            };
        }
    }
}