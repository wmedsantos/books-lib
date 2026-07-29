using BooksLib.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace BooksLib.Api.Features.Books;

public static class PublicCatalogEndpoints
{
    public static RouteGroupBuilder MapPublicCatalog(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/public/books").WithTags("Public Catalog");

        group.MapGet("/", ListPublishedBooks).AllowAnonymous().WithName("ListPublishedBooks").WithOpenApi();
        group.MapGet("/{id:guid}", GetPublishedBook).AllowAnonymous().WithName("GetPublishedBook").WithOpenApi();

        return api;
    }

    private static async Task<Ok<BookListResponse>> ListPublishedBooks(
        CatalogDbContext db,
        string? search,
        Guid? authorId,
        Guid? genreId,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = PublishedBooks(db);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = Book.NormalizeKey(search);
            query = query.Where(book => book.NormalizedTitle.Contains(normalizedSearch));
        }

        if (authorId is not null)
        {
            query = query.Where(book => book.AuthorId == authorId);
        }

        if (genreId is not null)
        {
            query = query.Where(book => book.GenreId == genreId);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(book => book.Title)
            .ThenBy(book => book.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(book => new BookResponse(
                book.Id,
                book.Title,
                book.AuthorId,
                book.Author.Name,
                book.GenreId,
                book.Genre.Name,
                book.CreatorCredit,
                book.Isbn13,
                book.Isbn10,
                book.Description,
                book.Publisher,
                book.PublishedOn,
                book.PageCount,
                book.CopyCount,
                book.CoverUrl,
                book.CollectionName,
                book.SourceAddedOn,
                book.PublishOnSite))
            .ToListAsync();

        return TypedResults.Ok(new BookListResponse(items, page, pageSize, total));
    }

    private static async Task<Results<Ok<BookResponse>, NotFound>> GetPublishedBook(CatalogDbContext db, Guid id)
    {
        var book = await PublishedBooks(db)
            .Where(book => book.Id == id)
            .Select(book => new BookResponse(
                book.Id,
                book.Title,
                book.AuthorId,
                book.Author.Name,
                book.GenreId,
                book.Genre.Name,
                book.CreatorCredit,
                book.Isbn13,
                book.Isbn10,
                book.Description,
                book.Publisher,
                book.PublishedOn,
                book.PageCount,
                book.CopyCount,
                book.CoverUrl,
                book.CollectionName,
                book.SourceAddedOn,
                book.PublishOnSite))
            .SingleOrDefaultAsync();

        return book is null ? TypedResults.NotFound() : TypedResults.Ok(book);
    }

    private static IQueryable<Book> PublishedBooks(CatalogDbContext db)
    {
        return db.Books
            .AsNoTracking()
            .Include(book => book.Author)
            .Include(book => book.Genre)
            .Where(book =>
                book.DeletedAtUtc == null &&
                book.PublishOnSite &&
                book.Author.DeletedAtUtc == null &&
                book.Genre.DeletedAtUtc == null);
    }
}
