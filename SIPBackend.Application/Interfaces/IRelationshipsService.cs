using SIPBackend.Domain.Dtos;

namespace SIPBackend.Application.Interfaces;

public interface IRelationshipsService
{
    /*
     * MakeRelationship GetAllRelationships DeleteFriend FindFriend AcceptRequest GetAllRequests
     */

    Task<ResponseDto> MakeRelationship(MakeRelationshipDto dto);
    Task<ResponseDto<IReadOnlyList<FriendDto>>> GetAllRelationships();
    Task<ResponseDto> DeleteFriend(FriendWithIdDto withIdDto);
    Task<ResponseDto<FriendInfo>> FindFriend(FindFriendDto dto);
    Task<ResponseDto> AcceptFriendRequest(FriendWithIdDto withIdDto);
    Task<ResponseDto<IReadOnlyList<FriendDto>>> GetAllRequests();
}