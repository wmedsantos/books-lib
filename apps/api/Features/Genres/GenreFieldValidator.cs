using BooksLib.Api.Features;

namespace BooksLib.Api.Features.Genres;

public static class GenreFieldValidator
{
    public static Dictionary<string, string[]> Validate(GenreRequest request)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.AddError("name", "Name is required.");
        }
        else if (Genre.NormalizeDisplayName(request.Name).Length > 120)
        {
            errors.AddError("name", "Name must be 120 characters or fewer.");
        }

        return errors.ToProblemErrors();
    }
}
