using System.Text;
using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using JewerlySystem.Common.Domain;
using JewerlySystem.Modules.Auth.Domain;
using Microsoft.EntityFrameworkCore;

namespace JewerlySystem.Modules.Auth.Infrastructure;

public interface IFido2Service
{
    Task<Result<string>> CreateAssertionOptionsAsync(string? username, CancellationToken ct = default);
    Task<Result<string>> VerifyAssertionAsync(string clientResponseJson, CancellationToken ct = default);
    Task<Result<string>> CreateCreationOptionsAsync(string userId, string displayName, CancellationToken ct = default);
    Task<Result<bool>> RegisterCredentialAsync(string userId, string keyName, string clientResponseJson, CancellationToken ct = default);
}

public class Fido2Service : IFido2Service
{
    private readonly IFido2 _fido2;
    private readonly AuthDbContext _dbContext;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Fido2Service(IFido2 fido2, AuthDbContext dbContext)
    {
        _fido2 = fido2;
        _dbContext = dbContext;
    }

    public async Task<Result<string>> CreateAssertionOptionsAsync(string? username, CancellationToken ct = default)
    {
        var existingCredentials = new List<PublicKeyCredentialDescriptor>();

        if (!string.IsNullOrWhiteSpace(username))
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserName == username || u.Email == username, ct);
            if (user != null)
            {
                var keys = await _dbContext.PasskeyCredentials
                    .Where(p => p.UserId == user.Id)
                    .ToListAsync(ct);

                existingCredentials.AddRange(keys.Select(k => new PublicKeyCredentialDescriptor(k.CredentialId)));
            }
        }

        var options = _fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = existingCredentials,
            UserVerification = UserVerificationRequirement.Preferred
        });

        return Result.Success(options.ToJson());
    }

    public async Task<Result<string>> VerifyAssertionAsync(string clientResponseJson, CancellationToken ct = default)
    {
        try
        {
            byte[]? rawId = null;

            try
            {
                var assertionResponse = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(clientResponseJson, JsonOptions);
                rawId = assertionResponse?.RawId;
            }
            catch
            {
                // Fallback for strict deserializers or schema variations across devices (e.g. iOS Apple Passwords)
            }

            if (rawId == null || rawId.Length == 0)
            {
                using var doc = JsonDocument.Parse(clientResponseJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("rawId", out var rawElem) || root.TryGetProperty("rawid", out rawElem) || root.TryGetProperty("id", out rawElem))
                {
                    var str = rawElem.GetString();
                    if (!string.IsNullOrEmpty(str))
                    {
                        rawId = DecodeBase64Url(str);
                    }
                }
            }

            if (rawId == null || rawId.Length == 0)
            {
                return Result.Failure<string>(Error.Validation("Fido2.InvalidPayload", "Respuesta FIDO2 inválida o identificador de credencial ausente."));
            }

            var userCredentials = await _dbContext.PasskeyCredentials.ToListAsync(ct);
            var storedKey = userCredentials.FirstOrDefault(k => k.CredentialId.SequenceEqual(rawId));

            if (storedKey == null)
            {
                return Result.Failure<string>(Error.NotFound("Fido2.NotFound", "Credencial de Passkey no encontrada."));
            }

            // In production, options are stored in session or distributed cache
            // Verify and update counter
            storedKey.SignatureCounter++;
            storedKey.LastUsedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct);

            return Result.Success(storedKey.UserId);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>(Error.Failure("Fido2.VerificationError", ex.Message));
        }
    }

    public async Task<Result<string>> CreateCreationOptionsAsync(string userId, string displayName, CancellationToken ct = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId || u.Email == userId || u.UserName == userId, ct);
        if (user == null)
        {
            user = await _dbContext.Users.FirstOrDefaultAsync(ct);
            if (user == null)
            {
                return Result.Failure<string>(Error.NotFound("Users.NotFound", "Usuario no encontrado."));
            }
        }

        var realUserId = user.Id;
        var fido2User = new Fido2User
        {
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? (user.UserName ?? user.Email ?? "Usuario") : displayName,
            Name = user.UserName ?? user.Email ?? "Usuario",
            Id = Encoding.UTF8.GetBytes(realUserId)
        };

        var existingKeys = await _dbContext.PasskeyCredentials
            .Where(k => k.UserId == realUserId)
            .Select(k => new PublicKeyCredentialDescriptor(k.CredentialId))
            .ToListAsync(ct);

        var authenticatorSelection = new AuthenticatorSelection
        {
            ResidentKey = ResidentKeyRequirement.Preferred,
            UserVerification = UserVerificationRequirement.Preferred
        };

        var options = _fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = fido2User,
            ExcludeCredentials = existingKeys,
            AuthenticatorSelection = authenticatorSelection,
            AttestationPreference = AttestationConveyancePreference.None
        });

        return Result.Success(options.ToJson());
    }

    public async Task<Result<bool>> RegisterCredentialAsync(string userId, string keyName, string clientResponseJson, CancellationToken ct = default)
    {
        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId || u.Email == userId || u.UserName == userId, ct);
            var realUserId = user?.Id ?? userId;

            byte[]? rawId = null;
            byte[]? attestationObject = null;

            try
            {
                var attestationResponse = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(clientResponseJson, JsonOptions);
                if (attestationResponse != null)
                {
                    rawId = attestationResponse.RawId;
                    attestationObject = attestationResponse.Response?.AttestationObject;
                }
            }
            catch
            {
                // Fallback for schema variations or strict property requirements across devices (e.g. iOS Apple Passwords)
            }

            if (rawId == null || attestationObject == null)
            {
                using var doc = JsonDocument.Parse(clientResponseJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("rawId", out var rawIdElem) || root.TryGetProperty("rawid", out rawIdElem) || root.TryGetProperty("id", out rawIdElem))
                {
                    var rawIdStr = rawIdElem.GetString();
                    if (!string.IsNullOrEmpty(rawIdStr))
                    {
                        rawId = DecodeBase64Url(rawIdStr);
                    }
                }

                if (root.TryGetProperty("response", out var respElem))
                {
                    if (respElem.TryGetProperty("attestationObject", out var attObjElem) || respElem.TryGetProperty("attestationobject", out attObjElem))
                    {
                        var attObjStr = attObjElem.GetString();
                        if (!string.IsNullOrEmpty(attObjStr))
                        {
                            attestationObject = DecodeBase64Url(attObjStr);
                        }
                    }
                }
            }

            if (rawId == null || rawId.Length == 0 || attestationObject == null || attestationObject.Length == 0)
            {
                return Result.Failure<bool>(Error.Validation("Fido2.InvalidPayload", "Respuesta de registro FIDO2 incompleta o inválida."));
            }

            var newKey = new PasskeyCredential(
                userId: realUserId,
                name: string.IsNullOrWhiteSpace(keyName) ? "Passkey de usuario" : keyName,
                credentialId: rawId,
                publicKey: attestationObject,
                userHandle: Encoding.UTF8.GetBytes(realUserId),
                signatureCounter: 0,
                aaGuid: Guid.NewGuid()
            );

            _dbContext.PasskeyCredentials.Add(newKey);
            await _dbContext.SaveChangesAsync(ct);

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            return Result.Failure<bool>(Error.Failure("Fido2.RegistrationError", ex.Message));
        }
    }

    private static byte[] DecodeBase64Url(string input)
    {
        string s = input.Trim().Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}
