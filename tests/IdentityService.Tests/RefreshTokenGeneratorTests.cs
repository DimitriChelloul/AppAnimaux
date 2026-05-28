using IdentityService.BLL.Security;

namespace IdentityService.Tests;

public sealed class RefreshTokenGeneratorTests
{
    [Fact]
    public void Generate_ReturnsDifferentOpaqueTokens()
    {
        var generator = new RefreshTokenGenerator();

        var first = generator.Generate();
        var second = generator.Generate();

        Assert.NotEqual(first, second);
        Assert.DoesNotContain("+", first);
        Assert.DoesNotContain("/", first);
    }

    [Fact]
    public void Hash_ReturnsStableHash_ForSameToken()
    {
        var generator = new RefreshTokenGenerator();
        const string token = "refresh-token";

        Assert.Equal(generator.Hash(token), generator.Hash(token));
    }
}
