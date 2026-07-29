using BooksLib.Api.Features.Books;

namespace BooksLib.Api.Tests.Books;

public sealed class BookFieldValidatorTests
{
    private static readonly Guid AuthorId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid GenreId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Validate_requires_required_fields()
    {
        var errors = BookFieldValidator.Validate(ValidRequest() with
        {
            Title = " ",
            AuthorId = null,
            GenreId = null,
            CopyCount = null
        });

        Assert.Contains("Title is required.", errors["title"]);
        Assert.Contains("Author is required.", errors["authorId"]);
        Assert.Contains("Genre is required.", errors["genreId"]);
        Assert.Contains("Copy count must be at least 1.", errors["copyCount"]);
    }

    [Fact]
    public void Validate_rejects_title_over_240_characters()
    {
        var errors = BookFieldValidator.Validate(ValidRequest() with { Title = new string('T', 241) });

        Assert.Contains("Title must be 240 characters or fewer.", errors["title"]);
    }

    [Fact]
    public void Validate_accepts_title_at_240_characters()
    {
        var errors = BookFieldValidator.Validate(ValidRequest() with { Title = new string('T', 240) });

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_rejects_missing_or_non_positive_copy_count(int? copyCount)
    {
        var errors = BookFieldValidator.Validate(ValidRequest() with { CopyCount = copyCount });

        Assert.Contains("Copy count must be at least 1.", errors["copyCount"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_rejects_non_positive_page_count(int pageCount)
    {
        var errors = BookFieldValidator.Validate(ValidRequest() with { PageCount = pageCount });

        Assert.Contains("Page count must be positive when provided.", errors["pageCount"]);
    }

    [Theory]
    [InlineData("creatorCredit", 501, "creatorCredit must be 500 characters or fewer.")]
    [InlineData("description", 4001, "description must be 4000 characters or fewer.")]
    [InlineData("publisher", 241, "publisher must be 240 characters or fewer.")]
    [InlineData("collectionName", 241, "collectionName must be 240 characters or fewer.")]
    [InlineData("coverUrl", 1001, "coverUrl must be 1000 characters or fewer.")]
    public void Validate_rejects_text_fields_over_max_length(string field, int length, string expected)
    {
        var request = field switch
        {
            "creatorCredit" => ValidRequest() with { CreatorCredit = new string('C', length) },
            "description" => ValidRequest() with { Description = new string('D', length) },
            "publisher" => ValidRequest() with { Publisher = new string('P', length) },
            "collectionName" => ValidRequest() with { CollectionName = new string('C', length) },
            "coverUrl" => ValidRequest() with { CoverUrl = "https://" + new string('c', length - 12) + ".com" },
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
        };

        var errors = BookFieldValidator.Validate(request);

        Assert.Contains(expected, errors[field]);
    }

    [Fact]
    public void Validate_accepts_text_fields_at_max_length()
    {
        var request = ValidRequest() with
        {
            CreatorCredit = new string('C', 500),
            Description = new string('D', 4000),
            Publisher = new string('P', 240),
            CollectionName = new string('C', 240),
            CoverUrl = "https://example.com/" + new string('c', 980)
        };

        var errors = BookFieldValidator.Validate(request);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("isbn13", "978853470531", "isbn13 must contain exactly 13 digits.")]
    [InlineData("isbn13", "978853470531X", "isbn13 must contain exactly 13 digits.")]
    [InlineData("isbn10", "853470531", "isbn10 must contain exactly 10 digits.")]
    [InlineData("isbn10", "853470531X", "isbn10 must contain exactly 10 digits.")]
    public void Validate_rejects_isbn_with_wrong_length_or_non_digits(string field, string value, string expected)
    {
        var request = field == "isbn13"
            ? ValidRequest() with { Isbn13 = value }
            : ValidRequest() with { Isbn10 = value };

        var errors = BookFieldValidator.Validate(request);

        Assert.Contains(expected, errors[field]);
    }

    [Fact]
    public void Validate_accepts_valid_isbn_values()
    {
        var errors = BookFieldValidator.Validate(ValidRequest() with
        {
            Isbn13 = "9788534705317",
            Isbn10 = "8534705317"
        });

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_rejects_relative_cover_url()
    {
        var errors = BookFieldValidator.Validate(ValidRequest() with { CoverUrl = "/cover.jpg" });

        Assert.Contains("Cover URL must be absolute when provided.", errors["coverUrl"]);
    }

    [Fact]
    public void Validate_rejects_non_https_cover_url()
    {
        var errors = BookFieldValidator.Validate(ValidRequest() with { CoverUrl = "http://example.com/cover.jpg" });

        Assert.Contains("Cover URL must use HTTPS.", errors["coverUrl"]);
    }

    [Fact]
    public void Validate_accepts_valid_request()
    {
        var errors = BookFieldValidator.Validate(ValidRequest());

        Assert.Empty(errors);
    }

    private static BookRequest ValidRequest()
        => new(
            "Poncia Vicencio",
            AuthorId,
            GenreId,
            "Maria da Conceicao Evaristo",
            "9788534705317",
            "8534705317",
            "Novel",
            "Mazza Edicoes",
            new DateOnly(2003, 1, 1),
            128,
            1,
            "https://example.com/cover.jpg",
            "Biblioteca",
            new DateOnly(2024, 1, 1),
            false);
}
