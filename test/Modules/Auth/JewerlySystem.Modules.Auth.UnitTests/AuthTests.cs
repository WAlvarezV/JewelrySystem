using JewerlySystem.Modules.Auth.Application;
using Xunit;

namespace JewerlySystem.Modules.Auth.UnitTests;

public class AuthTests
{
    [Fact]
    public void LoginWithPasswordCommand_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var command = new LoginWithPasswordCommand("admin", "P@ssword123", true);

        // Assert
        Assert.Equal("admin", command.EmailOrUserName);
        Assert.Equal("P@ssword123", command.Password);
        Assert.True(command.RememberMe);
    }

    [Fact]
    public void LoginWithMfaCommand_ShouldInitializeWithCode()
    {
        // Arrange & Act
        var command = new LoginWithMfaCommand("123456", false);

        // Assert
        Assert.Equal("123456", command.TwoFactorCode);
        Assert.False(command.RememberMachine);
    }

    [Fact]
    public void GetPasskeyAssertionOptionsQuery_ShouldAllowNullUsernameForResidentKeys()
    {
        // Arrange & Act
        var query = new GetPasskeyAssertionOptionsQuery(null);

        // Assert
        Assert.Null(query.Username);
    }

    [Fact]
    public void RegisterPasskeyCommand_ShouldStoreUserIdAndKeyName()
    {
        // Arrange & Act
        var command = new RegisterPasskeyCommand("user-1", "iPhone 15 FaceID", "{}");

        // Assert
        Assert.Equal("user-1", command.UserId);
        Assert.Equal("iPhone 15 FaceID", command.KeyName);
        Assert.Equal("{}", command.AttestationResponseJson);
    }
}
