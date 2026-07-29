using BooksLib.Api.Features.Authors;

namespace BooksLib.Api.Tests.Authors;

public sealed class AuthorFieldValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_requires_name(string? name)
    {
        var errors = AuthorFieldValidator.Validate(new AuthorRequest(name));

        Assert.Contains("Name is required.", errors["name"]);
    }

    [Fact]
    public void Validate_rejects_name_over_160_characters()
    {
        var errors = AuthorFieldValidator.Validate(new AuthorRequest(new string('A', 161)));

        Assert.Contains("Name must be 160 characters or fewer.", errors["name"]);
    }

    [Fact]
    public void Validate_accepts_name_at_160_characters()
    {
        var errors = AuthorFieldValidator.Validate(new AuthorRequest(new string('A', 160)));

        Assert.Empty(errors);
    }
}
