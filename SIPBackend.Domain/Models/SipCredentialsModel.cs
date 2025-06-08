namespace SIPBackend.Domain.Dtos;

public sealed class SipCredentialsModel
{
    public string SipUsername { get; set; } = string.Empty;
    public string SipPassword { get; set; }  = string.Empty;
    public string SipDomain { get; set; } = string.Empty;
}