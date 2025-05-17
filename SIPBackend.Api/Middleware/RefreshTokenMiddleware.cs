using SIPBackend.Application.Interfaces;
using SIPBackend.Domain.Models;


namespace WebStoreMVC.Middleware;

public class RefreshTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;
    private const int AccessTokenLivingTime = 15; //60 - 45, есть 45 мин чтобы обновить токен за каждые 15 минут
    private const int RefreshThresholdMinutes = CookieInfo.AccessTokenExpiresTime - AccessTokenLivingTime; // Время до истечения, когда нужно обновить токен 

    public RefreshTokenMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
    {
        _next = next;
        _serviceProvider = serviceProvider;
    }

    public async Task Invoke(HttpContext context)
    {
        string accessToken = context.Request.Cookies[CookieInfo.accessToken] ?? string.Empty;
        string refreshToken = context.Request.Cookies[CookieInfo.refreshToken] ?? string.Empty;
        //Время для обновления токена за AccessTokenLivingTime
        var accessTokenExpires = DateTime.UtcNow.AddMinutes(CookieInfo.AccessTokenExpiresTime + 1);

        // Проверяем, существует ли токен вообще
        if (string.IsNullOrEmpty(refreshToken))
        {
            context.Response.Cookies.Delete(CookieInfo.refreshToken);
            context.Response.Cookies.Delete(CookieInfo.accessToken);
            await _next(context);
            
            return;
        }

        if (string.IsNullOrEmpty(accessToken))
        {
            accessTokenExpires = DateTime.UtcNow;
        }
        
        //Вычисляем время, когда нужно обновить токен (за RefreshThresholdMinutes до истечения)
        DateTimeOffset accessTokenExpiry;
        if (!TryGetAccessTokenExpiryFromCookie(accessToken, out accessTokenExpiry))
        {
            // Не удалось распарсить время истечения токена из куки, пропускаем обновление
            await _next(context);
            
            return;
        }

        var refreshTime = accessTokenExpiry.AddMinutes(-RefreshThresholdMinutes);

        // обновляем токен раз в 15 минут при выполнении запроса, есть 45 минут для обновления токена
        // если выходим за это время то пользователь становится не авторизован на 1 запрос
        if (DateTime.UtcNow >= refreshTime.UtcDateTime || DateTime.UtcNow >= accessTokenExpires)
        {
            using var scope = _serviceProvider.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
            var user = await authService.GetUserByRefreshToken(refreshToken);
            if (user.Data != null)
            {
                //Refresh Token Rotation
                var newAccessToken = await authService.SetAccessTokenForMiddleware(user.Data);
                
                var refreshTokenFromCookies = context.Request.Cookies[CookieInfo.refreshToken];
                if (refreshToken != refreshTokenFromCookies)
                {
                    await authService.Logout();
                }
            }
            else
            {
                await authService.Logout();
            }
        }

        await _next(context);
    }

    private bool TryGetAccessTokenExpiryFromCookie(string accessToken, out DateTimeOffset expiry)
    {
        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(accessToken);
            var expiryClaim = token.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;

            if (expiryClaim != null && long.TryParse(expiryClaim, out long unixExpiry))
            {
                expiry = DateTimeOffset.FromUnixTimeSeconds(unixExpiry);
                
                return true;
            }
        }
        catch
        {
            expiry = DateTimeOffset.MinValue;
            
            return false;
        }

        expiry = DateTimeOffset.MinValue;
        
        return false;
    }
}