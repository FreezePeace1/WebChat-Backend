using System.ComponentModel.DataAnnotations;

namespace SIPBackend.Domain.Dtos;

public record FriendInfo([Required] string UserName,string Id);