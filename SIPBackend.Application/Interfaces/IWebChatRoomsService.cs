using SIPBackend.Domain.Dtos;
using SIPBackend.Domain.Entities;

namespace SIPBackend.Application.Interfaces;

public interface IWebChatRoomsService
{
    Task<ResponseDto<WebChatRoom>> GetUsersWebChatRoomAsync(GetUsersWebChatRoomDto dto);
}