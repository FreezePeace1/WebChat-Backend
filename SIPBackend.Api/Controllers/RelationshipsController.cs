using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIPBackend.Application.Interfaces;
using SIPBackend.Domain.Dtos;

namespace SIPBackend.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class RelationshipsController : ControllerBase
{
    private readonly IRelationshipsService _relationshipsService;
    
    public RelationshipsController(IRelationshipsService relationshipsService)
    {
        _relationshipsService = relationshipsService;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ResponseDto>> MakeRelationship(MakeRelationshipDto dto)
    {
        var result = await _relationshipsService.MakeRelationship(dto);

        if (result.IsSucceed)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ResponseDto<IReadOnlyList<FriendDto>>>> GetAllRelationships()
    {
        var result = await _relationshipsService.GetAllRelationships();

        if (result.IsSucceed)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpDelete]
    [Authorize]
    public async Task<ActionResult<ResponseDto>> DeleteFriend(FriendWithIdDto withIdDto)
    {
        var result = await _relationshipsService.DeleteFriend(withIdDto);

        if (result.IsSucceed)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpGet]
    public async Task<ActionResult<ResponseDto>> FindFriend(FindFriendDto dto)
    {
        var result = await _relationshipsService.FindFriend(dto);

        if (result.IsSucceed)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpPut]
    [Authorize]
    public async Task<ActionResult<ResponseDto>> AcceptFriendRequest(FriendWithIdDto withIdDto)
    {
        var result = await _relationshipsService.AcceptFriendRequest(withIdDto);

        if (result.IsSucceed)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ResponseDto<IReadOnlyList<FriendDto>>>> GetAllRequests()
    {
        var result = await _relationshipsService.GetAllRequests();

        if (result.IsSucceed)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }
}