using Microsoft.AspNetCore.Mvc;
using SIPBackend.Application.Interfaces;
using SIPBackend.Domain.Dtos;
using SIPBackend.Domain.Entities;

namespace SIPBackend.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class CallingStoryController : ControllerBase
{
    private readonly ICallingStoryService _callingStoryService;

    public CallingStoryController(ICallingStoryService callingStoryService)
    {
        _callingStoryService = callingStoryService;
    }

    [HttpGet]
    public async Task<ActionResult<ResponseDto<IReadOnlyList<CallingStory>>>> GetUserCallingStories(CancellationToken ct)
    {
        var response = await _callingStoryService.GetUserCallingStoriesAsync(ct);

        if (response.IsSucceed)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [HttpPost]
    public async Task<ActionResult<ResponseDto>> StartUsersCallingStory(CallingStoryDto dto)
    {
        var response = await _callingStoryService.StartUsersCallingStoryAsync(dto);

        if (response.IsSucceed)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [HttpPut]
    public async Task<ActionResult<ResponseDto>> EndUsersCallingStory(CallingStoryDto dto)
    {
        var response = await _callingStoryService.EndUsersCallingStoryAsync(dto);

        if (response.IsSucceed)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }
}