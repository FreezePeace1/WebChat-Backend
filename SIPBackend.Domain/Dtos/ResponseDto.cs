namespace SIPBackend.Domain.Dtos;

public class ResponseDto
{
    public string? ErrorMessage { get; set; }
    public int? ErrorCode { get; set; }
    
    public string? SuccessMessage { get; set; }

    public bool IsSucceed => ErrorMessage == null;
}

public sealed class ResponseDto<T> : ResponseDto where T : class
{
    public ResponseDto(int errorCode, string errorMessage,string successMessage)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        SuccessMessage = successMessage;
    }
    
    public ResponseDto()
    {
        
    }
    
    public T Data { get; set; }
}