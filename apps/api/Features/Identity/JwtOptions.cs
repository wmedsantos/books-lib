namespace BooksLib.Api.Features.Identity;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "BooksLib";
    public string Audience { get; init; } = "BooksLib.Web";
    public string SigningKey { get; init; } = "";
    public int ExpirationMinutes { get; init; } = 60;
}
