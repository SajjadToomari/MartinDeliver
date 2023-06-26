using System.Security.Claims;

namespace WebAPI.Services;

public interface IAntiForgeryCookieService
{
    void RegenerateAntiForgeryCookies(IEnumerable<Claim> claims);
    void DeleteAntiForgeryCookies();
}