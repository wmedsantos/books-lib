using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BooksLib.Api.Features.Authors;

public sealed class Author
{
    private Author()
    {
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public string NormalizedName { get; private set; } = "";
    public string? SystemCode { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public bool IsSystem => SystemCode is not null;
    public bool IsDeleted => DeletedAtUtc is not null;

    public static Author Create(string name)
    {
        return new Author
        {
            Id = Guid.NewGuid(),
            Name = NormalizeDisplayName(name),
            NormalizedName = NormalizeKey(name),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public static Author CreateSystem(string name, string systemCode)
    {
        var author = Create(name);
        author.SystemCode = systemCode;
        return author;
    }

    public void Rename(string name)
    {
        if (IsSystem)
        {
            throw new InvalidOperationException("System authors cannot be renamed.");
        }

        Name = NormalizeDisplayName(name);
        NormalizedName = NormalizeKey(name);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        if (IsSystem)
        {
            throw new InvalidOperationException("System authors cannot be deleted.");
        }

        DeletedAtUtc = DateTimeOffset.UtcNow;
    }

    public static string NormalizeDisplayName(string value) => value.Trim();

    public static string NormalizeKey(string value) => value.Trim().ToUpperInvariant();

    public static class SystemCodes
    {
        public const string NotIdentified = "not-identified";
    }
}

public sealed class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.ToTable("authors");
        builder.HasKey(author => author.Id);

        builder.Property(author => author.Id).HasColumnName("id");
        builder.Property(author => author.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
        builder.Property(author => author.NormalizedName).HasColumnName("normalized_name").HasMaxLength(160).IsRequired();
        builder.Property(author => author.SystemCode).HasColumnName("system_code").HasMaxLength(80);
        builder.Property(author => author.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(author => author.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(author => author.DeletedAtUtc).HasColumnName("deleted_at_utc");

        builder.HasIndex(author => author.NormalizedName)
            .IsUnique()
            .HasFilter("deleted_at_utc IS NULL");

        builder.HasIndex(author => author.SystemCode)
            .IsUnique()
            .HasFilter("system_code IS NOT NULL");
    }
}
