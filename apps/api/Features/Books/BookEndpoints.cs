using System.Security.Claims;
using BooksLib.Api.Data;
using BooksLib.Api.Features.Audit;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace BooksLib.Api.Features.Books;

public static class BookEndpoints
{
    public static RouteGroupBuilder MapBooks(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/books").WithTags("Books");

        group.MapGet("/", ListBooks).WithName("ListBooks").WithOpenApi();
        group.MapGet("/{id:guid}", GetBook).WithName("GetBook").WithOpenApi();
        group.MapPost("/", CreateBook).RequireAuthorization("CatalogWrite").WithName("CreateBook").WithOpenApi();
        group.MapPut("/{id:guid}", UpdateBook).RequireAuthorization("CatalogWrite").WithName("UpdateBook").WithOpenApi();
        group.MapDelete("/{id:guid}", DeleteBook).RequireAuthorization("CatalogWrite").WithName("DeleteBook").WithOpenApi();

        return api;
    }

    private static async Task<Ok<BookListResponse>> ListBooks(
        CatalogDbContext db,
        string? search,
        Guid? authorId,
        Guid? genreId,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Books
            .AsNoTracking()
            .Include(book => book.Author)
            .Include(book => book.Genre)
            .Where(book => book.DeletedAtUtc == null);

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
            .Select(book => book.ToResponse())
            .ToListAsync();

        return TypedResults.Ok(new BookListResponse(items, page, pageSize, total));
    }

    private static async Task<Results<Ok<BookResponse>, NotFound>> GetBook(CatalogDbContext db, Guid id)
    {
        var book = await db.Books
            .AsNoTracking()
            .Include(book => book.Author)
            .Include(book => book.Genre)
            .Where(book => book.Id == id && book.DeletedAtUtc == null)
            .Select(book => book.ToResponse())
            .SingleOrDefaultAsync();

        return book is null ? TypedResults.NotFound() : TypedResults.Ok(book);
    }

    private static async Task<Results<Created<BookResponse>, ValidationProblem, Conflict<HttpValidationProblemDetails>>> CreateBook(
        CatalogDbContext db,
        BookRequest request)
    {
        var draft = await ValidateBookAsync(db, request);
        if (draft.Validation is not null)
        {
            return draft.Validation;
        }

        if (draft.Conflict is not null)
        {
            return draft.Conflict;
        }

        var book = Book.Create(draft.Value!);
        db.Books.Add(book);
        await db.SaveChangesAsync();

        var response = await LoadResponseAsync(db, book.Id);
        return TypedResults.Created($"/api/v1/books/{book.Id}", response!);
    }

    private static async Task<Results<Ok<BookResponse>, NotFound, ValidationProblem, Conflict<HttpValidationProblemDetails>>> UpdateBook(
        CatalogDbContext db,
        Guid id,
        BookRequest request)
    {
        var book = await db.Books.SingleOrDefaultAsync(book => book.Id == id && book.DeletedAtUtc == null);
        if (book is null)
        {
            return TypedResults.NotFound();
        }

        var draft = await ValidateBookAsync(db, request);
        if (draft.Validation is not null)
        {
            return draft.Validation;
        }

        if (draft.Conflict is not null)
        {
            return draft.Conflict;
        }

        book.Update(draft.Value!);
        await db.SaveChangesAsync();

        var response = await LoadResponseAsync(db, book.Id);
        return TypedResults.Ok(response!);
    }

    private static async Task<Results<NoContent, NotFound>> DeleteBook(
        CatalogDbContext db,
        ClaimsPrincipal principal,
        Guid id)
    {
        var book = await db.Books.SingleOrDefaultAsync(book => book.Id == id && book.DeletedAtUtc == null);
        if (book is null)
        {
            return TypedResults.NotFound();
        }

        book.SoftDelete();
        db.AuditEntries.Add(AuditEntry.Create(principal.Identity?.Name ?? "unknown", "Book", id, "SoftDelete"));
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<BookValidationResult> ValidateBookAsync(CatalogDbContext db, BookRequest request)
    {
        var errors = BookFieldValidator.Validate(request);

        if (errors.Count > 0)
        {
            return new(null, TypedResults.ValidationProblem(errors), null);
        }

        var authorId = request.AuthorId.GetValueOrDefault();
        var genreId = request.GenreId.GetValueOrDefault();
        var copyCount = request.CopyCount.GetValueOrDefault();

        var authorExists = await db.Authors.AnyAsync(author =>
            author.Id == authorId && author.DeletedAtUtc == null);

        if (!authorExists)
        {
            return new(null, null, Conflict("Author is invalid.", "Book author must exist and be active."));
        }

        var genreExists = await db.Genres.AnyAsync(genre =>
            genre.Id == genreId && genre.DeletedAtUtc == null);

        if (!genreExists)
        {
            return new(null, null, Conflict("Genre is invalid.", "Book genre must exist and be active."));
        }

        return new(new BookDraft(
            request.Title!,
            authorId,
            genreId,
            request.CreatorCredit,
            request.Isbn13,
            request.Isbn10,
            request.Description,
            request.Publisher,
            request.PublishedOn,
            request.PageCount,
            copyCount,
            request.CoverUrl,
            request.CollectionName,
            request.SourceAddedOn,
            request.PublishOnSite), null, null);
    }

    private static async Task<BookResponse?> LoadResponseAsync(CatalogDbContext db, Guid id)
    {
        return await db.Books
            .AsNoTracking()
            .Include(book => book.Author)
            .Include(book => book.Genre)
            .Where(book => book.Id == id)
            .Select(book => book.ToResponse())
            .SingleOrDefaultAsync();
    }

    private static Conflict<HttpValidationProblemDetails> Conflict(string title, string detail)
    {
        return TypedResults.Conflict(new HttpValidationProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = StatusCodes.Status409Conflict
        });
    }

    private static BookResponse ToResponse(this Book book)
        => new(
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
            book.PublishOnSite);

    private sealed record BookValidationResult(
        BookDraft? Value,
        ValidationProblem? Validation,
        Conflict<HttpValidationProblemDetails>? Conflict);
}
