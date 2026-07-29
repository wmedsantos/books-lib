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
        ValidateIsbn(errors, "isbn13", request.Isbn13, 13);
        ValidateIsbn(errors, "isbn10", request.Isbn10, 10);
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

    private static void ValidateIsbn(
        Dictionary<string, List<string>> errors,
        string field,
        string? value,
        int length)
    {
        var normalized = Book.NormalizeDisplayText(value);
        if (normalized is not null && (normalized.Length != length || normalized.Any(character => !char.IsDigit(character))))
        {
            errors.AddError(field, $"{field} must contain exactly {length} digits.");
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
