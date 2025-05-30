using Microsoft.AspNetCore.Mvc;
using SIPBackend.Application.Interfaces;
using SIPBackend.Domain.Dtos;
using SIPBackend.Domain.Entities;

namespace SIPBackend.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class WebChatRoomController : ControllerBase
{
    private readonly IWebChatRoomsService _chatRoomsService;
    
    public WebChatRoomController(IWebChatRoomsService chatRoomsService)
    {
        _chatRoomsService = chatRoomsService;
    }

    [HttpPost]
    public async Task<ActionResult<ResponseDto<WebChatRoom>>> GetUsersWebChatRoom(GetUsersWebChatRoomDto dto)
    {
        var response = await _chatRoomsService.GetUsersWebChatRoomAsync(dto);

        if (response.IsSucceed)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }
}