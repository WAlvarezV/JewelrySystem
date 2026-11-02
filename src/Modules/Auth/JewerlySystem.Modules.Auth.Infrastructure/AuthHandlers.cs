using JewerlySystem.Common.Application;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Auth.Application;
using Microsoft.AspNetCore.Identity;

namespace JewerlySystem.Modules.Auth.Infrastructure;

public class LoginWithPasswordCommandHandler : ICommandHandler<LoginWithPasswordCommand, LoginResponseDto>
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public LoginWithPasswordCommandHandler(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task<Result<LoginResponseDto>> Handle(LoginWithPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.EmailOrUserName)
            ?? await _userManager.FindByEmailAsync(request.EmailOrUserName);

        if (user == null)
        {
            return Result.Failure<LoginResponseDto>(Error.Unauthorized("Auth.InvalidCredentials", "Credenciales incorrectas."));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            return Result.Success(new LoginResponseDto(true, false, false, user.Id));
        }

        if (result.RequiresTwoFactor)
        {
            return Result.Success(new LoginResponseDto(false, true, false, user.Id, "Requiere verificación de segundo factor (MFA)."));
        }

        if (result.IsLockedOut)
        {
            return Result.Failure<LoginResponseDto>(Error.Unauthorized("Auth.LockedOut", "La cuenta se encuentra bloqueada por intentos fallidos."));
        }

        return Result.Failure<LoginResponseDto>(Error.Unauthorized("Auth.InvalidCredentials", "Usuario o contraseña inválidos."));
    }
}

public class GenerateMfaQrCodeQueryHandler : IQueryHandler<GenerateMfaQrCodeQuery, MfaSetupDto>
{
    private readonly IMfaService _mfaService;
    public GenerateMfaQrCodeQueryHandler(IMfaService mfaService) => _mfaService = mfaService;

    public async Task<Result<MfaSetupDto>> Handle(GenerateMfaQrCodeQuery request, CancellationToken cancellationToken) =>
        await _mfaService.GenerateMfaQrCodeAsync(request.UserId);
}

public class GetMfaStatusQueryHandler : IQueryHandler<GetMfaStatusQuery, MfaStatusDto>
{
    private readonly IMfaService _mfaService;
    public GetMfaStatusQueryHandler(IMfaService mfaService) => _mfaService = mfaService;

    public async Task<Result<MfaStatusDto>> Handle(GetMfaStatusQuery request, CancellationToken cancellationToken) =>
        await _mfaService.GetMfaStatusAsync(request.UserId);
}

public class EnableMfaCommandHandler : ICommandHandler<EnableMfaCommand, IReadOnlyList<string>>
{
    private readonly IMfaService _mfaService;
    public EnableMfaCommandHandler(IMfaService mfaService) => _mfaService = mfaService;

    public async Task<Result<IReadOnlyList<string>>> Handle(EnableMfaCommand request, CancellationToken cancellationToken) =>
        await _mfaService.VerifyAndEnableMfaAsync(request.UserId, request.Code);
}

public class DisableMfaCommandHandler : ICommandHandler<DisableMfaCommand, bool>
{
    private readonly IMfaService _mfaService;
    public DisableMfaCommandHandler(IMfaService mfaService) => _mfaService = mfaService;

    public async Task<Result<bool>> Handle(DisableMfaCommand request, CancellationToken cancellationToken) =>
        await _mfaService.DisableMfaAsync(request.UserId);
}

public class GenerateNewRecoveryCodesCommandHandler : ICommandHandler<GenerateNewRecoveryCodesCommand, IReadOnlyList<string>>
{
    private readonly IMfaService _mfaService;
    public GenerateNewRecoveryCodesCommandHandler(IMfaService mfaService) => _mfaService = mfaService;

    public async Task<Result<IReadOnlyList<string>>> Handle(GenerateNewRecoveryCodesCommand request, CancellationToken cancellationToken) =>
        await _mfaService.GenerateNewRecoveryCodesAsync(request.UserId);
}

public class LoginWithMfaCommandHandler : ICommandHandler<LoginWithMfaCommand, bool>
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    public LoginWithMfaCommandHandler(SignInManager<ApplicationUser> signInManager) => _signInManager = signInManager;

    public async Task<Result<bool>> Handle(LoginWithMfaCommand request, CancellationToken cancellationToken)
    {
        var cleanCode = request.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty).Trim();
        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(cleanCode, isPersistent: false, rememberClient: request.RememberMachine);
        if (result.Succeeded)
        {
            return Result.Success(true);
        }

        var recoveryResult = await _signInManager.TwoFactorRecoveryCodeSignInAsync(cleanCode);
        if (recoveryResult.Succeeded)
        {
            return Result.Success(true);
        }

        return Result.Failure<bool>(Error.Validation("Mfa.InvalidCode", "Código de autenticación o recuperación inválido."));
    }
}

public class PasskeyHandlers :
    IQueryHandler<GetPasskeyAssertionOptionsQuery, string>,
    ICommandHandler<VerifyPasskeyAssertionCommand, bool>,
    IQueryHandler<GetPasskeyCreationOptionsQuery, string>,
    ICommandHandler<RegisterPasskeyCommand, bool>
{
    private readonly IFido2Service _fido2Service;
    public PasskeyHandlers(IFido2Service fido2Service) => _fido2Service = fido2Service;

    public async Task<Result<string>> Handle(GetPasskeyAssertionOptionsQuery request, CancellationToken cancellationToken) =>
        await _fido2Service.CreateAssertionOptionsAsync(request.Username, cancellationToken);

    public async Task<Result<bool>> Handle(VerifyPasskeyAssertionCommand request, CancellationToken cancellationToken)
    {
        var result = await _fido2Service.VerifyAssertionAsync(request.AssertionResponseJson, cancellationToken);
        return result.IsSuccess ? Result.Success(true) : Result.Failure<bool>(result.Error);
    }

    public async Task<Result<string>> Handle(GetPasskeyCreationOptionsQuery request, CancellationToken cancellationToken) =>
        await _fido2Service.CreateCreationOptionsAsync(request.UserId, request.DisplayName, cancellationToken);

    public async Task<Result<bool>> Handle(RegisterPasskeyCommand request, CancellationToken cancellationToken) =>
        await _fido2Service.RegisterCredentialAsync(request.UserId, request.KeyName, request.AttestationResponseJson, cancellationToken);
}
