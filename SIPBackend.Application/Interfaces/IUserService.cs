using SIPBackend.Domain.Dtos;

namespace SIPBackend.Application.Interfaces;

public interface IUserService
{
    Task<ResponseDto<SipCredentialsModel>> SipCredentials();
}