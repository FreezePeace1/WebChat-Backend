using SIPBackend.Domain.Dtos;
using SIPBackend.Domain.Entities;

namespace SIPBackend.Application.Interfaces;

public interface ICallingStoryService
{
    Task<ResponseDto<IReadOnlyList<CallingStory>>> GetUserCallingStoriesAsync(CancellationToken ct);
    Task<ResponseDto> StartUsersCallingStoryAsync(CallingStoryDto dto);
    
    Task<ResponseDto> EndUsersCallingStoryAsync(CallingStoryDto dto);
}