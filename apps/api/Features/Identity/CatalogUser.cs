using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BooksLib.Api.Features.Identity;

public sealed class CatalogUser
{
    private CatalogUser()
    {
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = "";
    public string NormalizedEmail { get; private set; } = "";
    public string PasswordHash { get; private set; } = "";
    public bool PasswordChangeRequired { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public static CatalogUser Create(string email)
    {
        return new CatalogUser
        {
            Id = Guid.NewGuid(),
            Email = email.Trim(),
            NormalizedEmail = NormalizeEmail(email),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void SetPasswordHash(string passwordHash, bool requirePasswordChange)
    {
        PasswordHash = passwordHash;
        PasswordChangeRequired = requirePasswordChange;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();
}

public sealed class CatalogUserConfiguration : IEntityTypeConfiguration<CatalogUser>
{
    public void Configure(EntityTypeBuilder<CatalogUser> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id).HasColumnName("id");
        builder.Property(user => user.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
        builder.Property(user => user.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(254).IsRequired();
        builder.Property(user => user.PasswordHash).HasColumnName("password_hash").HasMaxLength(500).IsRequired();
        builder.Property(user => user.PasswordChangeRequired).HasColumnName("password_change_required").IsRequired();
        builder.Property(user => user.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(user => user.UpdatedAtUtc).HasColumnName("updated_at_utc");

        builder.HasIndex(user => user.NormalizedEmail).IsUnique();
    }
}
