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

public class AuthDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<PasskeyCredential> PasskeyCredentials => Set<PasskeyCredential>();

    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new PasskeyCredentialConfig());
    }
}
