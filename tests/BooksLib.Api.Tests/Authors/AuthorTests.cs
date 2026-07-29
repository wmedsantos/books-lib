using BooksLib.Api.Features.Authors;

namespace BooksLib.Api.Tests.Authors;

public sealed class AuthorTests
{
    [Fact]
    public void Create_trims_display_name_and_normalizes_key()
    {
        var author = Author.Create("  Conceicao Evaristo  ");

        Assert.Equal("Conceicao Evaristo", author.Name);
        Assert.Equal("CONCEICAO EVARISTO", author.NormalizedName);
        Assert.False(author.IsSystem);
    }

    [Fact]
    public void System_author_cannot_be_renamed_or_deleted()
    {
        var author = Author.CreateSystem("Not Identified", Author.SystemCodes.NotIdentified);

        Assert.True(author.IsSystem);
        Assert.Equal(Author.SystemCodes.NotIdentified, author.SystemCode);
        Assert.Throws<InvalidOperationException>(() => author.Rename("Other"));
        Assert.Throws<InvalidOperationException>(() => author.SoftDelete());
    }
}
