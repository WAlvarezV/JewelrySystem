using System.Text.Encodings.Web;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Auth.Application;
using Microsoft.AspNetCore.Identity;
using QRCoder;

namespace JewerlySystem.Modules.Auth.Infrastructure;

public interface IMfaService
{
    Task<Result<MfaSetupDto>> GenerateMfaQrCodeAsync(string userId);
    Task<Result<IReadOnlyList<string>>> VerifyAndEnableMfaAsync(string userId, string code);
    Task<Result<bool>> VerifyMfaCodeAsync(string userId, string code);
}

public class MfaService : IMfaService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UrlEncoder _urlEncoder;

    public MfaService(UserManager<ApplicationUser> userManager, UrlEncoder urlEncoder)
    {
        _userManager = userManager;
        _urlEncoder = urlEncoder;
    }

    public async Task<Result<MfaSetupDto>> GenerateMfaQrCodeAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result.Failure<MfaSetupDto>(Error.NotFound("Users.NotFound", "Usuario no encontrado."));

        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        var email = user.Email ?? user.UserName ?? "usuario";
        var authenticatorUri = string.Format(
            "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
            _urlEncoder.Encode("JewerlySystem"),
            _urlEncoder.Encode(email),
            unformattedKey);

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(20);
        var qrCodeBase64 = Convert.ToBase64String(qrCodeBytes);

        return Result.Success(new MfaSetupDto(unformattedKey!, authenticatorUri, qrCodeBase64));
    }

    public async Task<Result<IReadOnlyList<string>>> VerifyAndEnableMfaAsync(string userId, string code)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result.Failure<IReadOnlyList<string>>(Error.NotFound("Users.NotFound", "Usuario no encontrado."));

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            code);

        if (!isValid)
            return Result.Failure<IReadOnlyList<string>>(Error.Validation("Mfa.InvalidCode", "Código de verificación incorrecto."));

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

        return Result.Success<IReadOnlyList<string>>(recoveryCodes?.ToList() ?? new List<string>());
    }

    public async Task<Result<bool>> VerifyMfaCodeAsync(string userId, string code)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Result.Failure<bool>(Error.NotFound("Users.NotFound", "Usuario no encontrado."));

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            code);

        return isValid
            ? Result.Success(true)
            : Result.Failure<bool>(Error.Validation("Mfa.InvalidCode", "Código de autenticación inválido."));
    }
}
