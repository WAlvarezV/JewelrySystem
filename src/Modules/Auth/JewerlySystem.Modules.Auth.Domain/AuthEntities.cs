using JewerlySystem.Common.Domain;

namespace JewerlySystem.Modules.Auth.Domain;

public class User : AuditableEntity<string>
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public bool TwoFactorEnabled { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    public User() { }

    public User(string id, string userName, string email) : base(id)
    {
        UserName = userName;
        Email = email;
        IsActive = true;
    }
}

public class PasskeyCredential
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public byte[] CredentialId { get; set; } = Array.Empty<byte>();
    public byte[] PublicKey { get; set; } = Array.Empty<byte>();
    public byte[] UserHandle { get; set; } = Array.Empty<byte>();
    public uint SignatureCounter { get; set; }
    public string CredType { get; set; } = "public-key";
    public Guid AaGuid { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }

    public PasskeyCredential() { }

    public PasskeyCredential(
        string userId,
        string name,
        byte[] credentialId,
        byte[] publicKey,
        byte[] userHandle,
        uint signatureCounter,
        Guid aaGuid)
    {
        UserId = userId;
        Name = name;
        CredentialId = credentialId;
        PublicKey = publicKey;
        UserHandle = userHandle;
        SignatureCounter = signatureCounter;
        AaGuid = aaGuid;
        CreatedAt = DateTime.UtcNow;
    }
}
