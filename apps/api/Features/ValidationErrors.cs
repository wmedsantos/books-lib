namespace BooksLib.Api.Features;

public static class ValidationErrors
{
    public static void AddError(this Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var messages))
        {
            messages = [];
            errors[field] = messages;
        }

        messages.Add(message);
    }

    public static Dictionary<string, string[]> ToProblemErrors(this Dictionary<string, List<string>> errors)
    {
        return errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
    }
}
