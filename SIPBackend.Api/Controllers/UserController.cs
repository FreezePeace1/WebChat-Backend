using Microsoft.AspNetCore.Mvc;
using SIPBackend.Application.Interfaces;
using SIPBackend.Domain.Dtos;

namespace SIPBackend.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    
    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<ResponseDto<SipCredentialsModel>>> SipCredentials()
    {
        var response = await _userService.SipCredentials();

        if (response.IsSucceed)
        {
            return Ok(response);
        }
        
        return BadRequest(response);
    }

}