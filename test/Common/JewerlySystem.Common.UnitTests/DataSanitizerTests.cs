using JewerlySystem.Common.Application.Security;
using Xunit;

namespace JewerlySystem.Common.UnitTests;

public class DataSanitizerTests
{
    [Theory]
    [InlineData("usuario@example.com", "u***o@example.com")]
    [InlineData("admin@jewelry.co", "a***n@jewelry.co")]
    [InlineData("ab@domain.com", "a*@domain.com")]
    public void MaskEmail_ShouldMaskEmailCorrectly(string email, string expected)
    {
        var result = DataSanitizer.MaskEmail(email);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1020304050", "******4050")]
    [InlineData("1234", "****")]
    [InlineData("987654321", "*****4321")]
    public void MaskIdentification_ShouldMaskIdentificationNumbers(string doc, string expected)
    {
        var result = DataSanitizer.MaskIdentification(doc);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("LoginCommand", true)]
    [InlineData("ChangePasswordCommand", true)]
    [InlineData("VerifyPasskeyAssertionCommand", true)]
    [InlineData("CreateContractCommand", false)]
    [InlineData("GetInventoryItemsQuery", false)]
    public void IsSensitive_ShouldIdentifySensitiveRequestTypes(string typeName, bool expected)
    {
        var result = DataSanitizer.IsSensitive(typeName);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SanitizeForLog_ShouldRedactSensitivePayloads()
    {
        var sensitiveObj = new DummyLoginCommand("admin", "SuperSecret123!");
        var result = DataSanitizer.SanitizeForLog(sensitiveObj);

        Assert.Contains("REDACTED", result);
        Assert.DoesNotContain("SuperSecret123!", result);
    }

    private record DummyLoginCommand(string Username, string Password);
}
