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

    [Fact]
    public void MfaStatusDto_ShouldHoldCorrectState()
    {
        // Arrange & Act
        var status = new MfaStatusDto(IsEnabled: true, HasAuthenticatorKey: true, RecoveryCodesLeft: 8);

        // Assert
        Assert.True(status.IsEnabled);
        Assert.True(status.HasAuthenticatorKey);
        Assert.Equal(8, status.RecoveryCodesLeft);
    }

    [Fact]
    public void MfaQueriesAndCommands_ShouldInitializeProperly()
    {
        // Arrange & Act
        var getQuery = new GetMfaStatusQuery("user-123");
        var genQuery = new GenerateMfaQrCodeQuery("user-123");
        var enableCmd = new EnableMfaCommand("user-123", "654321");
        var disableCmd = new DisableMfaCommand("user-123");
        var recoveryCmd = new GenerateNewRecoveryCodesCommand("user-123");

        // Assert
        Assert.Equal("user-123", getQuery.UserId);
        Assert.Equal("user-123", genQuery.UserId);
        Assert.Equal("user-123", enableCmd.UserId);
        Assert.Equal("654321", enableCmd.Code);
        Assert.Equal("user-123", disableCmd.UserId);
        Assert.Equal("user-123", recoveryCmd.UserId);
    }
}
