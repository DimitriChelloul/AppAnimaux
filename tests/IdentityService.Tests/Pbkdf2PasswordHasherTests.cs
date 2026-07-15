using Shared.Security;

namespace IdentityService.Tests;

public sealed class Pbkdf2PasswordHasherTests
{
    [Fact]
    public void Verify_ReturnsTrue_ForMatchingPassword()
    {
        var hasher = new Pbkdf2PasswordHasher();

        var hash = hasher.Hash("CorrectHorseBatteryStaple");

        Assert.True(hasher.Verify("CorrectHorseBatteryStaple", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        var hasher = new Pbkdf2PasswordHasher();

        var hash = hasher.Hash("CorrectHorseBatteryStaple");

        Assert.False(hasher.Verify("wrong-password", hash));
    }
}
