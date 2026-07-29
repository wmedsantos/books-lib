using BooksLib.Api.Features.Genres;

namespace BooksLib.Api.Tests.Genres;

public sealed class GenreTests
{
    [Fact]
    public void Create_trims_display_name_and_normalizes_key()
    {
        var genre = Genre.Create("  Poetry  ");

        Assert.Equal("Poetry", genre.Name);
        Assert.Equal("POETRY", genre.NormalizedName);
        Assert.False(genre.IsSystem);
        Assert.False(genre.IsDeleted);
    }

    [Fact]
    public void System_genre_cannot_be_renamed_or_deleted()
    {
        var genre = Genre.CreateSystem("Unclassified", Genre.SystemCodes.Unclassified);

        Assert.True(genre.IsSystem);
        Assert.Equal(Genre.SystemCodes.Unclassified, genre.SystemCode);
        Assert.Throws<InvalidOperationException>(() => genre.Rename("Other"));
        Assert.Throws<InvalidOperationException>(() => genre.SoftDelete());
    }

    [Fact]
    public void Custom_genre_can_be_renamed_and_soft_deleted()
    {
        var genre = Genre.Create("History");

        genre.Rename("Afro Brazilian History");
        genre.SoftDelete();

        Assert.Equal("Afro Brazilian History", genre.Name);
        Assert.Equal("AFRO BRAZILIAN HISTORY", genre.NormalizedName);
        Assert.True(genre.IsDeleted);
    }
}
