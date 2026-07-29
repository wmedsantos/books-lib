using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BooksLib.Api.Features.Genres;

public sealed class Genre
{
    private Genre()
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

    public static Genre Create(string name)
    {
        var now = DateTimeOffset.UtcNow;

        return new Genre
        {
            Id = Guid.NewGuid(),
            Name = NormalizeDisplayName(name),
            NormalizedName = NormalizeKey(name),
            CreatedAtUtc = now
        };
    }

    public static Genre CreateSystem(string name, string systemCode)
    {
        var genre = Create(name);
        genre.SystemCode = systemCode;
        return genre;
    }

    public void Rename(string name)
    {
        if (IsSystem)
        {
            throw new InvalidOperationException("System genres cannot be renamed.");
        }

        Name = NormalizeDisplayName(name);
        NormalizedName = NormalizeKey(name);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        if (IsSystem)
        {
            throw new InvalidOperationException("System genres cannot be deleted.");
        }

        DeletedAtUtc = DateTimeOffset.UtcNow;
    }

    public static string NormalizeDisplayName(string value) => value.Trim();

    public static string NormalizeKey(string value) => value.Trim().ToUpperInvariant();

    public static class SystemCodes
    {
        public const string Unclassified = "unclassified";
    }
}

public sealed class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
    public void Configure(EntityTypeBuilder<Genre> builder)
    {
        builder.ToTable("genres");
        builder.HasKey(genre => genre.Id);

        builder.Property(genre => genre.Id).HasColumnName("id");
        builder.Property(genre => genre.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(genre => genre.NormalizedName).HasColumnName("normalized_name").HasMaxLength(120).IsRequired();
        builder.Property(genre => genre.SystemCode).HasColumnName("system_code").HasMaxLength(80);
        builder.Property(genre => genre.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(genre => genre.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(genre => genre.DeletedAtUtc).HasColumnName("deleted_at_utc");

        builder.HasIndex(genre => genre.NormalizedName)
            .IsUnique()
            .HasFilter("deleted_at_utc IS NULL");

        builder.HasIndex(genre => genre.SystemCode)
            .IsUnique()
            .HasFilter("system_code IS NOT NULL");
    }
}
