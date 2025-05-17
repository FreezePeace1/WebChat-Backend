using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIPBackend.Application.Interfaces;
using SIPBackend.Domain.Dtos;
using SIPBackend.Domain.Models;

namespace SIPBackend.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost]
    public async Task<ActionResult<ResponseDto>> Register(RegistrationDto dto)
    {
        var result = await _authService.Registration(dto);

        if (result.IsSucceed)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    [HttpPost]
    public async Task<ActionResult<ResponseDto>> Login(LoginDto dto)
    {
        var result = await _authService.Login(dto);

        if (result.IsSucceed)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpPost]
    public async Task<ActionResult<ResponseDto>> Logout()
    {
        var accessToken = HttpContext.Request.Cookies[$"{CookieInfo.accessToken}"];
        var refreshToken = HttpContext.Request.Cookies[$"{CookieInfo.refreshToken}"];

        if (string.IsNullOrEmpty(accessToken) && string.IsNullOrEmpty(refreshToken))
        {
            return RedirectToAction("Login");
        }
        
        var result = await _authService.Logout();

        if (result.IsSucceed)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpPut]
    public async Task<ActionResult<ResponseDto>> ForgotPassword(ForgotPasswordDto dto)
    {
        var result = await _authService.ForgotPassword(dto.Email);

        if (result.IsSucceed)
        {
            return RedirectToAction("ResetPassword");
        }

        return BadRequest(result);
    }

    [HttpPut]
    public async Task<ActionResult<ResponseDto>> ResetPassword(ResetPasswordDto dto)
    {
        var result = await _authService.ResetPassword(dto);

        if (result.IsSucceed)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpPut]
    public async Task<ActionResult<ResponseDto>> ChangeInfo(ChangeUserInfoDto dto)
    {
        var accessToken = HttpContext.Request.Cookies[$"{CookieInfo.accessToken}"];
        var refreshToken = HttpContext.Request.Cookies[$"{CookieInfo.refreshToken}"];

        if (string.IsNullOrEmpty(accessToken) && string.IsNullOrEmpty(refreshToken))
        {
            return RedirectToAction("Login");
        }
        
        var result = await _authService.ChangeInfo(dto);

        if (result.IsSucceed)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpGet]
    public async Task<ActionResult<ResponseDto>> Account()
    {
        var result = await _authService.Account();
        if (result.IsSucceed)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }
    
}