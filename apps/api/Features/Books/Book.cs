using BooksLib.Api.Features.Authors;
using BooksLib.Api.Features.Genres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BooksLib.Api.Features.Books;

public sealed class Book
{
    private Book()
    {
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = "";
    public string NormalizedTitle { get; private set; } = "";
    public Guid AuthorId { get; private set; }
    public Guid GenreId { get; private set; }
    public string? CreatorCredit { get; private set; }
    public string? Isbn13 { get; private set; }
    public string? Isbn10 { get; private set; }
    public string? Description { get; private set; }
    public string? Publisher { get; private set; }
    public DateOnly? PublishedOn { get; private set; }
    public int? PageCount { get; private set; }
    public int CopyCount { get; private set; }
    public string? CoverUrl { get; private set; }
    public string? CollectionName { get; private set; }
    public DateOnly? SourceAddedOn { get; private set; }
    public bool PublishOnSite { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public Author Author { get; private set; } = null!;
    public Genre Genre { get; private set; } = null!;
    public bool IsDeleted => DeletedAtUtc is not null;

    public static Book Create(BookDraft draft)
    {
        var book = new Book
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        book.Apply(draft);
        return book;
    }

    public void Update(BookDraft draft)
    {
        Apply(draft);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        DeletedAtUtc = DateTimeOffset.UtcNow;
    }

    private void Apply(BookDraft draft)
    {
        Title = NormalizeDisplayText(draft.Title) ?? "";
        NormalizedTitle = NormalizeKey(Title);
        AuthorId = draft.AuthorId;
        GenreId = draft.GenreId;
        CreatorCredit = NormalizeDisplayText(draft.CreatorCredit);
        Isbn13 = NormalizeDisplayText(draft.Isbn13);
        Isbn10 = NormalizeDisplayText(draft.Isbn10);
        Description = NormalizeDisplayText(draft.Description);
        Publisher = NormalizeDisplayText(draft.Publisher);
        PublishedOn = draft.PublishedOn;
        PageCount = draft.PageCount;
        CopyCount = draft.CopyCount;
        CoverUrl = NormalizeDisplayText(draft.CoverUrl);
        CollectionName = NormalizeDisplayText(draft.CollectionName);
        SourceAddedOn = draft.SourceAddedOn;
        PublishOnSite = draft.PublishOnSite;
    }

    public static string NormalizeKey(string value) => value.Trim().ToUpperInvariant();

    public static string? NormalizeDisplayText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}

public sealed record BookDraft(
    string Title,
    Guid AuthorId,
    Guid GenreId,
    string? CreatorCredit,
    string? Isbn13,
    string? Isbn10,
    string? Description,
    string? Publisher,
    DateOnly? PublishedOn,
    int? PageCount,
    int CopyCount,
    string? CoverUrl,
    string? CollectionName,
    DateOnly? SourceAddedOn,
    bool PublishOnSite);

public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("books");
        builder.HasKey(book => book.Id);

        builder.Property(book => book.Id).HasColumnName("id");
        builder.Property(book => book.Title).HasColumnName("title").HasMaxLength(240).IsRequired();
        builder.Property(book => book.NormalizedTitle).HasColumnName("normalized_title").HasMaxLength(240).IsRequired();
        builder.Property(book => book.AuthorId).HasColumnName("author_id");
        builder.Property(book => book.GenreId).HasColumnName("genre_id");
        builder.Property(book => book.CreatorCredit).HasColumnName("creator_credit").HasMaxLength(500);
        builder.Property(book => book.Isbn13).HasColumnName("isbn13").HasMaxLength(13);
        builder.Property(book => book.Isbn10).HasColumnName("isbn10").HasMaxLength(10);
        builder.Property(book => book.Description).HasColumnName("description").HasMaxLength(4000);
        builder.Property(book => book.Publisher).HasColumnName("publisher").HasMaxLength(240);
        builder.Property(book => book.PublishedOn).HasColumnName("published_on");
        builder.Property(book => book.PageCount).HasColumnName("page_count");
        builder.Property(book => book.CopyCount).HasColumnName("copy_count").IsRequired();
        builder.Property(book => book.CoverUrl).HasColumnName("cover_url").HasMaxLength(1000);
        builder.Property(book => book.CollectionName).HasColumnName("collection_name").HasMaxLength(240);
        builder.Property(book => book.SourceAddedOn).HasColumnName("source_added_on");
        builder.Property(book => book.PublishOnSite).HasColumnName("publish_on_site").IsRequired();
        builder.Property(book => book.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(book => book.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(book => book.DeletedAtUtc).HasColumnName("deleted_at_utc");

        builder.HasOne(book => book.Author)
            .WithMany()
            .HasForeignKey(book => book.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(book => book.Genre)
            .WithMany()
            .HasForeignKey(book => book.GenreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(book => book.NormalizedTitle);
        builder.HasIndex(book => book.AuthorId);
        builder.HasIndex(book => book.GenreId);
        builder.HasIndex(book => book.Isbn13).HasFilter("isbn13 IS NOT NULL");
        builder.HasIndex(book => book.Isbn10).HasFilter("isbn10 IS NOT NULL");
    }
}
