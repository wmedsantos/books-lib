using BooksLib.Api.Features.Genres;

namespace BooksLib.Api.Tests.Genres;

public sealed class GenreFieldValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_requires_name(string? name)
    {
        var errors = GenreFieldValidator.Validate(new GenreRequest(name));

        Assert.Contains("Name is required.", errors["name"]);
    }

    [Fact]
    public void Validate_rejects_name_over_120_characters()
    {
        var errors = GenreFieldValidator.Validate(new GenreRequest(new string('G', 121)));

        Assert.Contains("Name must be 120 characters or fewer.", errors["name"]);
    }

    [Fact]
    public void Validate_accepts_name_at_120_characters()
    {
        var errors = GenreFieldValidator.Validate(new GenreRequest(new string('G', 120)));

        Assert.Empty(errors);
    }
}
