using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using SIPBackend.Application.Interfaces;
using SIPBackend.DAL.Context;
using SIPBackend.Domain.Dtos;
using SIPBackend.Domain.Entities;
using SIPBackend.Domain.Models;

namespace SIPBackend.Application.Hubs;

[Authorize]
public class ChatHub : Hub<IChatClient>
{
    private readonly SIPBackendContext _dbContext;
    private readonly UserManager<AppUser> _userManager;
    private readonly IHttpContextAccessor _httpContext;
    private readonly IDistributedCache _cache;
    
    public ChatHub(
        SIPBackendContext dbContext,
        UserManager<AppUser> userManager,
        IHttpContextAccessor httpContext, IDistributedCache cache)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _httpContext = httpContext;
        _cache = cache;
    }

    private DistributedCacheEntryOptions GetCacheOptions()
    {
        return new DistributedCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(14)
        };
    }

    public override async Task OnConnectedAsync()
    {
        var user = await GetCurrentUser();
        if (user != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{user.Id}");
        }

        await base.OnConnectedAsync();
    }

    public async Task<ChatInfo> JoinChat(string friendId)
    {
        var user = await GetCurrentUser();
        var friend = await _userManager.FindByIdAsync(friendId);

        if (user == null || friend == null)
        {
            throw new HubException("User not found");
        }

        // Поиск существующего чата
        var chat = await FindOrCreateChat(user.Id, friend.Id);

        await Groups.AddToGroupAsync(Context.ConnectionId, chat.ChatId.ToString());
        await LoadChatHistory(chat.ChatId.ToString());

        return new ChatInfo()
        {
            ChatId = chat.ChatId.ToString(),
            ParticipantsUserNames = new List<string>() { user.UserName, friend.UserName }
        };
    }

    public async Task<List<MessageDto>> GetAllMessages(Guid chatId)
    {
        var cacheKey = $"chat_history_{chatId}";
        var cachedHistory = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedHistory))
        {
            return JsonSerializer.Deserialize<List<MessageDto>>(cachedHistory);
        }
        
        var query = from message in _dbContext.Messages
            join sender in _dbContext.Users on message.UserSenderId equals sender.Id
            where message.ChatId == chatId
            orderby message.SentAt
            select new MessageDto 
            {
                Content = message.Content,
                SentAt = message.SentAt,
                SenderName = sender.UserName,
                IsCurrentUser = false
            };

        var messages = await query.ToListAsync();
        
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(messages),GetCacheOptions());
        
        return messages;
    }
    public async Task SendMessage(string chatId, string message)
    {
        var user = await GetCurrentUser();
        if (user == null) return;

        if (!Guid.TryParse(chatId, out var parsedChatID))
        {
            return;
        }
        
        var chat = await _dbContext.Chats
            .Include(c => c.ChatParticipants)
            .FirstOrDefaultAsync(x => x.ChatId.ToString() == chatId);

        if (chat?.ChatParticipants == null)
        {
            return;
        }

        if (chat.ChatParticipants.FirstUserId != user.Id && 
            chat.ChatParticipants.SecondUserId != user.Id)
        {
            return; // Пользователь не участник
        }
     
        var consumerId = user.Id == chat.ChatParticipants.FirstUserId 
            ? chat.ChatParticipants.SecondUserId 
            : chat.ChatParticipants.FirstUserId;

        if (user.Id == consumerId)
        {
            return;
        }

        var messageEntity = new Message
        {
            ChatId = parsedChatID,
            Content = message,
            UserSenderId = user.Id,
            UserConsumerId = consumerId,
            SentAt = DateTime.UtcNow
        };

        _dbContext.Messages.Add(messageEntity);
        await _dbContext.SaveChangesAsync();
        
        await _cache.RemoveAsync($"chat_history_{chatId}");

        await Clients.Group(chatId)
            .ReceiveMessage(user.UserName, message);
    }

    private async Task<Chat> FindOrCreateChat(string userId, string friendId)
    {
        // Check for existing chat participants
        var chatParticipants = await _dbContext.ChatParticipants
            .FirstOrDefaultAsync(cp =>
                (cp.FirstUserId == userId && cp.SecondUserId == friendId) ||
                (cp.FirstUserId == friendId && cp.SecondUserId == userId));

        if (chatParticipants != null)
            return await _dbContext.Chats
                .Include(c => c.Messages)
                .FirstAsync(c => c.ChatParticipantsId == chatParticipants.ChatParticipantsId);

        // Create and save new ChatParticipants FIRST
        var newParticipants = new ChatParticipants
        {
            FirstUserId = userId,
            SecondUserId = friendId
        };

        _dbContext.ChatParticipants.Add(newParticipants);
        await _dbContext.SaveChangesAsync(); // Generates ChatParticipantsId

        // Now create the Chat with the valid ChatParticipantsId
        var newChat = new Chat
        {
            ChatId = Guid.NewGuid(),
            ChatParticipantsId = newParticipants.ChatParticipantsId, // Use the generated ID
            Messages = new List<Message>()
        };

        _dbContext.Chats.Add(newChat);
        await _dbContext.SaveChangesAsync();

        return newChat;
    }

    // Сделайте метод публичным
    public async Task LoadChatHistory(string chatIdString)
    {
        if (!Guid.TryParse(chatIdString, out Guid chatId))
        {
            throw new HubException("Некорректный ID чата");
        }

        var cacheKey = $"chat_history_{chatId}";
        var cachedHistory = await _cache.GetStringAsync(cacheKey);
        List<MessageDto>? dtos = null;
        
        if (!string.IsNullOrEmpty(cachedHistory))
        {
            dtos = JsonSerializer.Deserialize<List<MessageDto>>(cachedHistory);
        }

        if (dtos == null)
        {
            var query = from message in _dbContext.Messages
                join sender in _dbContext.Users 
                    on message.UserSenderId equals sender.Id into senderGroup
                from sender in senderGroup.DefaultIfEmpty()
                where message.ChatId == chatId
                orderby message.SentAt
                select new MessageDto {
                    Content = message.Content,
                    SentAt = message.SentAt,
                    SenderName = sender != null ? sender.UserName : "Неизвестный",
                    IsCurrentUser = false
                };

            dtos = await query.ToListAsync();
            
            await _cache.SetStringAsync(
                cacheKey, 
                JsonSerializer.Serialize(dtos), 
                GetCacheOptions() 
            );
            
        }
        
        await Clients.Caller.LoadMessageHistory(dtos);
    }

    private async Task<AppUser?> GetCurrentUser()
    {
        var userName = _httpContext.HttpContext?.User.Identity?.Name;
        return await _userManager.FindByNameAsync(userName);
    }
}