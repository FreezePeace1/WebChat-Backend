using SIPBackend.Domain.Dtos;
using SIPBackend.Domain.Entities;
using SIPBackend.Domain.Models;

namespace SIPBackend.Application.Interfaces;

public interface ICallingStoryService
{
    Task<ResponseDto<IReadOnlyList<CallingStoryModel>>> GetUserCallingStoriesAsync(CancellationToken ct);
    Task<ResponseDto> StartUsersCallingStoryAsync(CallingStoryDto dto);
    
    Task<ResponseDto> EndUsersCallingStoryAsync(CallingStoryDto dto);
}