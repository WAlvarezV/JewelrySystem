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

public class AppModule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MenuItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ModuleKey { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RequiredPermission { get; set; }
}

public class UserMenuPreference
{
    public string UserId { get; set; } = string.Empty;
    public string FavoriteMenuIdsJson { get; set; } = "[]";
    public string ModuleOrderKeysJson { get; set; } = "[]";
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class RolePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RoleName { get; set; } = string.Empty;
    public string ModuleKey { get; set; } = string.Empty;
    public string MenuRoute { get; set; } = string.Empty;
    public bool CanView { get; set; } = true;
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanAdmin { get; set; }
}
