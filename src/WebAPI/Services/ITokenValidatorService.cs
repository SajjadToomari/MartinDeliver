using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace WebAPI.Services;

public interface ITokenValidatorService
{
    Task ValidateAsync(TokenValidatedContext context);
}