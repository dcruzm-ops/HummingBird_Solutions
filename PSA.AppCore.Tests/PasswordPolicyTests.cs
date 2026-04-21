using PSA.AppCore.Services.Security;
using Xunit;

namespace PSA.AppCore.Tests;

public class PasswordPolicyTests
{
    private readonly IPasswordPolicy _policy = new PasswordPolicy();

    [Theory]
    [InlineData("Abcdef!12")]
    [InlineData("ABCDEFGHIJ1!")]
    [InlineData("abcdefghi1!")]
    [InlineData("Abcdefghij!!")]
    [InlineData("Abcdefghij12")]
    [InlineData("Abc!123")]
    public void IsValid_ReturnsFalse_ForInvalidPasswords(string password)
    {
        Assert.False(_policy.IsValid(password));
    }

    [Fact]
    public void IsValid_ReturnsTrue_ForStrongPassword()
    {
        Assert.True(_policy.IsValid("FincaSegura2026!"));
    }
}
