using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SIPBackend.Application.Resources;
using SIPBackend.DAL.Context;
using SIPBackend.DAL.Resources;
using SIPBackend.Domain;
using SIPBackend.Domain.Dtos;
using SIPBackend.Domain.Entities;
using SIPBackend.Domain.Models;
using StackExchange.Redis;

namespace SIPBackend.Application.Hubs;

public class ChatHub : Hub<IChatClient>
{
    private readonly SIPBackendContext _dbContext;
    private readonly UserManager<AppUser> _userManager;
    private readonly IHttpContextAccessor _httpContext;
    private readonly IDatabase _cacheDb;
    private readonly ILogger _logger;

    public ChatHub(SIPBackendContext dbContext, UserManager<AppUser> userManager,
        IHttpContextAccessor httpContext, ILogger logger)
    {
        var redis = ConnectionMultiplexer.Connect("Redis");
        _cacheDb = redis.GetDatabase();
        _dbContext = dbContext;
        _userManager = userManager;
        _httpContext = httpContext;
        _logger = logger;
    }

    public async Task<ResponseDto<ChatInfoDto>> JoinChatAsync(UserConnection userConnection, CancellationToken token)
    {
        try
        {
            var authUserName = _httpContext.HttpContext.User.Identity.Name;
            if (string.IsNullOrEmpty(authUserName))
            {
                return new ResponseDto<ChatInfoDto>()
                {
                    ErrorMessage = ErrorMessage.UserAreNotAuthorized,
                    ErrorCode = (int)ErrorCodes.UserAreNotAuthorized
                };
            }

            var user = await _userManager.FindByNameAsync(authUserName);

            if (user is null)
            {
                return new ResponseDto<ChatInfoDto>()
                {
                    ErrorMessage = ErrorMessage.UserNotExists,
                    ErrorCode = (int)ErrorCodes.UserNotExists
                };
            }

            var getChatParticipants =
                await _dbContext.ChatParticipants.FirstOrDefaultAsync(x =>
                    (x.FirstUserId == user.Id && x.SecondUserId == userConnection.ConsumerUserNameId)
                    || (x.SecondUserId == user.Id && x.FirstUserId == userConnection.ConsumerUserNameId), token);

            if (getChatParticipants is null)
            {
                //Create new chat
                var newParticipants = new ChatParticipants()
                {
                    FirstUserId = user.Id,
                    SecondUserId = userConnection.ConsumerUserName
                };

                var newChat = new Chat()
                {
                    ChatId = Guid.NewGuid(),
                    ChatParticipantsId = newParticipants.ChatParticipantsId,
                };

                await _dbContext.Chats.AddAsync(newChat, token);
                await _dbContext.ChatParticipants.AddAsync(newParticipants, token);

                await Groups.AddToGroupAsync(newChat.ChatId.ToString(),
                    $"{user.UserName}And{userConnection.ConsumerUserName}", token);

                await Clients.Group(newChat.ChatId.ToString())
                    .ReceiveMessage("System", "Добро пожаловать в чат!");

                return new ResponseDto<ChatInfoDto>()
                {
                    Data = new ChatInfoDto()
                    {
                        ConsumerUserName = userConnection.ConsumerUserName,
                        SenderUserName = user.UserName
                    },
                    SuccessMessage = SuccessMessage.GettingChatInfoIsSuccessful
                };
            }

            var getChatInfo =
                await _dbContext.Chats.FirstOrDefaultAsync(x =>
                    x.ChatParticipantsId == getChatParticipants.ChatParticipantsId, cancellationToken: token);

            await Groups.AddToGroupAsync(getChatInfo.ChatId.ToString(),
                $"{user.UserName}And{userConnection.ConsumerUserName}", token);

            var allMessages = await GetAllMessages(getChatInfo.ChatId);

            return new ResponseDto<ChatInfoDto>()
            {
                Data = new ChatInfoDto()
                {
                    Messages = allMessages.Data,
                    ConsumerUserName = userConnection.ConsumerUserName,
                    SenderUserName = user.UserName
                },
                SuccessMessage = SuccessMessage.GettingChatInfoIsSuccessful
            };
        }
        catch (Exception e)
        {
            _logger.Error(e, e.Message);

            return new ResponseDto<ChatInfoDto>()
            {
                ErrorMessage = ErrorMessage.GettingChatInfoIsFailed,
                ErrorCode = (int)ErrorCodes.GettingChatInfoIsFailed
            };
        }
    }

    private async Task<ResponseDto<List<Message>>> GetAllMessages(Guid chatId)
    {
        try
        {
            var cachedMessages = await _cacheDb.ListRangeAsync($"chat:{chatId}:messages");

            if (cachedMessages.Any())
            {
                var allMessages = cachedMessages.Select(x => JsonSerializer.Deserialize<Message>(x))
                    .ToList();

                return new ResponseDto<List<Message>>()
                {
                    Data = allMessages,
                    SuccessMessage = SuccessMessage.GettingAllMessagesIsSuccessfull
                };
            }

            var dbMessages = await _dbContext.Messages
                .Where(x => x.ChatId == chatId)
                .OrderByDescending(x => x.SentAt)
                .Take(100)
                .ToListAsync();

            return new ResponseDto<List<Message>>()
            {
                Data = dbMessages,
                SuccessMessage = SuccessMessage.GettingAllMessagesIsSuccessfull
            };
        }
        catch (Exception e)
        {
            _logger.Error(e, e.Message);

            return new ResponseDto<List<Message>>()
            {
                ErrorMessage = ErrorMessage.FailedToGetAllMessages,
                ErrorCode = (int)ErrorCodes.FailedToGetAllMessages
            };
        }
    }

    public async Task<ResponseDto<SendMessageDto>> SendMessage(SendMessageDto dto, CancellationToken token)
    {
        try
        {
            var userName = _httpContext.HttpContext.User.Identity.Name;

            if (string.IsNullOrEmpty(userName))
            {
                return new ResponseDto<SendMessageDto>()
                {
                    ErrorMessage = ErrorMessage.UserAreNotAuthorized,
                    ErrorCode = (int)ErrorCodes.UserAreNotAuthorized
                };
            }

            var sender = await _userManager.FindByNameAsync(userName);

            if (sender is null)
            {
                return new ResponseDto<SendMessageDto>()
                {
                    ErrorMessage = ErrorMessage.UserNotExists,
                    ErrorCode = (int)ErrorCodes.UserNotExists
                };
            }

            var chat = await _dbContext.Chats.FirstOrDefaultAsync(x => x.ChatId == dto.ChatId, token);

            if (chat is null)
            {
                return new ResponseDto<SendMessageDto>()
                {
                    ErrorMessage = ErrorMessage.ChatDoesNotExist,
                    ErrorCode = (int)ErrorCodes.ChatDoesNotExist
                };
            }

            var takeChatGroup = await _dbContext.ChatParticipants
                .FirstOrDefaultAsync(x => x.ChatParticipantsId == chat.ChatParticipantsId, token);

            if (takeChatGroup is null)
            {
                return new ResponseDto<SendMessageDto>()
                {
                    ErrorMessage = ErrorMessage.TheseChatParticipantsDoNotExist,
                    ErrorCode = (int)ErrorCodes.TheseChatParticipantsDoNotExist
                };
            }

            var consumerId = sender.Id == takeChatGroup.FirstUserId
                ? takeChatGroup.SecondUserId
                : takeChatGroup.FirstUserId;

            var message = new Message()
            {
                ChatId = dto.ChatId,
                MessageId = Guid.NewGuid(),
                Content = dto.Content,
                UserSenderId = sender.Id,
                UserConsumerId = consumerId
            };

            await _dbContext.Messages.AddAsync(message, token);
            await _dbContext.SaveChangesAsync(token);

            await _cacheDb.ListLeftPushAsync($"chat:{dto.ChatId}:messages", JsonSerializer.Serialize(message));
            await _cacheDb.KeyExpireAsync($"chat:{dto.ChatId}:messages", TimeSpan.FromDays(7));

            await Clients.Group(dto.ChatId.ToString())
                .ReceiveMessage(sender.UserName, dto.Content);

            return new ResponseDto<SendMessageDto>()
            {
                Data = new SendMessageDto()
                {
                    ChatId = dto.ChatId,
                    Content = dto.Content
                },
                SuccessMessage = SuccessMessage.MessageWasSentSuccessfully
            };
        }
        catch (Exception e)
        {
           _logger.Error(e,e.Message);

           return new ResponseDto<SendMessageDto>()
           {
                ErrorMessage = ErrorMessage.FailedToSendMessage,
                ErrorCode = (int)ErrorCodes.FailedToSendMessage
           };
        }
    }

    /*public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var stringConnection = await _cache.GetAsync(Context.ConnectionId);
        var connection = JsonSerializer.Deserialize<UserConnection>(stringConnection);

        if (connection is not null)
        {
            await _cache.RemoveAsync(Context.ConnectionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, connection.ChatRoom);

            await Clients
                .Group(connection.ChatRoom)
                .ReceiveMessage("Admin", $"{connection.ConsumerUserName} вышел из чата");
        }

        await base.OnDisconnectedAsync(exception);
    }*/
}