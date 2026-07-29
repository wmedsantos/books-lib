using BooksLib.Api.Features;

namespace BooksLib.Api.Features.Books;

public static class BookFieldValidator
{
    public static Dictionary<string, string[]> Validate(BookRequest request)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.AddError("title", "Title is required.");
        }
        else if (Book.NormalizeDisplayText(request.Title)!.Length > 240)
        {
            errors.AddError("title", "Title must be 240 characters or fewer.");
        }

        if (request.AuthorId is null)
        {
            errors.AddError("authorId", "Author is required.");
        }

        if (request.GenreId is null)
        {
            errors.AddError("genreId", "Genre is required.");
        }

        if (request.CopyCount is null or < 1)
        {
            errors.AddError("copyCount", "Copy count must be at least 1.");
        }

        if (request.PageCount is < 1)
        {
            errors.AddError("pageCount", "Page count must be positive when provided.");
        }

        ValidateLength(errors, "creatorCredit", request.CreatorCredit, 500);
        ValidateLength(errors, "description", request.Description, 4000);
        ValidateLength(errors, "publisher", request.Publisher, 240);
        ValidateLength(errors, "collectionName", request.CollectionName, 240);
        ValidateLength(errors, "coverUrl", request.CoverUrl, 1000);
        ValidateIsbn13(errors, request.Isbn13);
        ValidateIsbn10(errors, request.Isbn10);
        ValidateCoverUrl(errors, request.CoverUrl);

        return errors.ToProblemErrors();
    }

    private static void ValidateLength(
        Dictionary<string, List<string>> errors,
        string field,
        string? value,
        int maxLength)
    {
        var normalized = Book.NormalizeDisplayText(value);
        if (normalized is not null && normalized.Length > maxLength)
        {
            errors.AddError(field, $"{field} must be {maxLength} characters or fewer.");
        }
    }

    private static void ValidateIsbn13(Dictionary<string, List<string>> errors, string? value)
    {
        var normalized = Book.NormalizeDisplayText(value);
        if (normalized is not null && (normalized.Length != 13 || normalized.Any(character => !char.IsDigit(character))))
        {
            errors.AddError("isbn13", "isbn13 must contain exactly 13 digits.");
        }
    }

    private static void ValidateIsbn10(Dictionary<string, List<string>> errors, string? value)
    {
        var normalized = Book.NormalizeDisplayText(value);
        if (normalized is null)
        {
            return;
        }

        var hasValidCharacters = normalized
            .Select((character, index) => index == 9
                ? char.IsDigit(character) || char.ToUpperInvariant(character) == 'X'
                : char.IsDigit(character))
            .All(valid => valid);

        if (normalized.Length != 10 || !hasValidCharacters)
        {
            errors.AddError("isbn10", "isbn10 must contain exactly 10 digits or 9 digits followed by X.");
        }
    }

    private static void ValidateCoverUrl(Dictionary<string, List<string>> errors, string? value)
    {
        var coverUrl = Book.NormalizeDisplayText(value);
        if (coverUrl is null)
        {
            return;
        }

        if (!Uri.TryCreate(coverUrl, UriKind.Absolute, out var parsedCoverUri) ||
            string.IsNullOrWhiteSpace(parsedCoverUri.Host))
        {
            errors.AddError("coverUrl", "Cover URL must be absolute when provided.");
        }
        else if (parsedCoverUri.Scheme != Uri.UriSchemeHttps)
        {
            errors.AddError("coverUrl", "Cover URL must use HTTPS.");
        }
    }
}
