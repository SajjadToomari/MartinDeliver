namespace WebAPI.Services;

public interface ISecurityService
{
    string GetSha256Hash(string input);
    Guid CreateCryptographicallySecureGuid();
}