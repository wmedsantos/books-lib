using BooksLib.Api.Features.Books;

namespace BooksLib.Api.Tests.Books;

public sealed class BookTests
{
    [Fact]
    public void Create_normalizes_strings_and_keeps_required_relationships()
    {
        var authorId = Guid.NewGuid();
        var genreId = Guid.NewGuid();

        var book = Book.Create(new BookDraft(
            "  Poncia Vicencio  ",
            authorId,
            genreId,
            "  Maria da Conceicao Evaristo  ",
            "9788534705317",
            null,
            "  Novel  ",
            "",
            new DateOnly(2003, 1, 1),
            128,
            2,
            "https://example.com/cover.jpg",
            " Biblioteca ",
            null,
            false));

        Assert.Equal("Poncia Vicencio", book.Title);
        Assert.Equal("PONCIA VICENCIO", book.NormalizedTitle);
        Assert.Equal(authorId, book.AuthorId);
        Assert.Equal(genreId, book.GenreId);
        Assert.Equal("Maria da Conceicao Evaristo", book.CreatorCredit);
        Assert.Null(book.Publisher);
        Assert.Equal(2, book.CopyCount);
        Assert.False(book.PublishOnSite);
    }

    [Fact]
    public void SoftDelete_marks_book_without_changing_relationships()
    {
        var authorId = Guid.NewGuid();
        var genreId = Guid.NewGuid();
        var book = Book.Create(new BookDraft(
            "Title",
            authorId,
            genreId,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            1,
            null,
            null,
            null,
            false));

        book.SoftDelete();

        Assert.True(book.IsDeleted);
        Assert.Equal(authorId, book.AuthorId);
        Assert.Equal(genreId, book.GenreId);
    }
}
