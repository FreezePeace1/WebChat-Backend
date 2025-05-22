namespace SIPBackend.Domain.Models;

public class RTCIceCandidateInit
{
    public string candidate { get; set; }
    public string sdpMid { get; set; }
    public int? sdpMLineIndex { get; set; }  // Изменим на nullable int
    public string usernameFragment { get; set; }
}