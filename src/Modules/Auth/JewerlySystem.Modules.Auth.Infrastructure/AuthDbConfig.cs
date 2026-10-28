using JewerlySystem.Modules.Auth.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JewerlySystem.Modules.Auth.Infrastructure;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PasskeyCredentialConfig : IEntityTypeConfiguration<PasskeyCredential>
{
    public void Configure(EntityTypeBuilder<PasskeyCredential> builder)
    {
        builder.ToTable("PasskeyCredentials");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(p => p.Name)
            .HasMaxLength(200);

        builder.Property(p => p.CredType)
            .HasMaxLength(50);

        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.CredentialId);
    }
}

public class AppModuleConfig : IEntityTypeConfiguration<AppModule>
{
    public void Configure(EntityTypeBuilder<AppModule> builder)
    {
        builder.ToTable("AppModules");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Key).HasMaxLength(50).IsRequired();
        builder.Property(m => m.Name).HasMaxLength(100).IsRequired();
        builder.Property(m => m.Icon).HasMaxLength(100);
        builder.Property(m => m.Description).HasMaxLength(500);
        builder.HasIndex(m => m.Key).IsUnique();
    }
}

public class MenuItemConfig : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.ModuleKey).HasMaxLength(50).IsRequired();
        builder.Property(m => m.Name).HasMaxLength(150).IsRequired();
        builder.Property(m => m.Route).HasMaxLength(250);
        builder.Property(m => m.Icon).HasMaxLength(100);
        builder.HasIndex(m => m.ModuleKey);
        builder.HasIndex(m => m.ParentId);
    }
}

public class UserMenuPreferenceConfig : IEntityTypeConfiguration<UserMenuPreference>
{
    public void Configure(EntityTypeBuilder<UserMenuPreference> builder)
    {
        builder.ToTable("UserMenuPreferences");
        builder.HasKey(p => p.UserId);
        builder.Property(p => p.UserId).HasMaxLength(450);
        builder.Property(p => p.FavoriteMenuIdsJson).IsRequired();
        builder.Property(p => p.ModuleOrderKeysJson).IsRequired();
    }
}

public class RolePermissionConfig : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.RoleName).HasMaxLength(256).IsRequired();
        builder.Property(p => p.ModuleKey).HasMaxLength(50).IsRequired();
        builder.Property(p => p.MenuRoute).HasMaxLength(250).IsRequired();
        builder.HasIndex(p => new { p.RoleName, p.MenuRoute });
    }
}

public class AuthDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<PasskeyCredential> PasskeyCredentials => Set<PasskeyCredential>();
    public DbSet<AppModule> AppModules => Set<AppModule>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<UserMenuPreference> UserPreferences => Set<UserMenuPreference>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new PasskeyCredentialConfig());
        builder.ApplyConfiguration(new AppModuleConfig());
        builder.ApplyConfiguration(new MenuItemConfig());
        builder.ApplyConfiguration(new UserMenuPreferenceConfig());
        builder.ApplyConfiguration(new RolePermissionConfig());
    }
}
