using WebAPI.DomainClasses;
using WebAPI.Models.Identity;

namespace WebAPI.Services;

public interface ITokenFactoryService
{
    Task<JwtTokensData> CreateJwtTokensAsync(User user);
    string? GetRefreshTokenSerial(string refreshTokenValue);
}