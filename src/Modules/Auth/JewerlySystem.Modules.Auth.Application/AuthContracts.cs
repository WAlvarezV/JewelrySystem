using JewerlySystem.Common.Application;

namespace JewerlySystem.Modules.Auth.Application;

public record LoginResponseDto(
    bool Succeeded,
    bool RequiresTwoFactor,
    bool IsLockedOut,
    string? UserId = null,
    string? Message = null);

public record MfaSetupDto(
    string SharedKey,
    string AuthenticatorUri,
    string QrCodeBase64);

public record LoginWithPasswordCommand(string EmailOrUserName, string Password, bool RememberMe = false)
    : ICommand<LoginResponseDto>;

public record LoginWithMfaCommand(string TwoFactorCode, bool RememberMachine = false)
    : ICommand<bool>;

public record GenerateMfaQrCodeQuery(string UserId)
    : IQuery<MfaSetupDto>;

public record EnableMfaCommand(string UserId, string Code)
    : ICommand<IReadOnlyList<string>>;

public record GetPasskeyAssertionOptionsQuery(string? Username)
    : IQuery<string>;

public record VerifyPasskeyAssertionCommand(string AssertionResponseJson)
    : ICommand<bool>;

public record GetPasskeyCreationOptionsQuery(string UserId, string DisplayName)
    : IQuery<string>;

public record RegisterPasskeyCommand(string UserId, string KeyName, string AttestationResponseJson)
    : ICommand<bool>;
