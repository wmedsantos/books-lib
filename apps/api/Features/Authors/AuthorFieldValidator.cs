using BooksLib.Api.Features;

namespace BooksLib.Api.Features.Authors;

public static class AuthorFieldValidator
{
    public static Dictionary<string, string[]> Validate(AuthorRequest request)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.AddError("name", "Name is required.");
        }
        else if (Author.NormalizeDisplayName(request.Name).Length > 160)
        {
            errors.AddError("name", "Name must be 160 characters or fewer.");
        }

        return errors.ToProblemErrors();
    }
}
