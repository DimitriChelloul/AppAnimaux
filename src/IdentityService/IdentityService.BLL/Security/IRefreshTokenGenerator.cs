namespace IdentityService.BLL.Security;

public interface IRefreshTokenGenerator
{
    string Generate();
    string Hash(string token);
}
