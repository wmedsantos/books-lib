using BooksLib.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace BooksLib.Api.Features.Authors;

public static class AuthorEndpoints
{
    public static RouteGroupBuilder MapAuthors(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/authors").WithTags("Authors");

        group.MapGet("/", ListAuthors).WithName("ListAuthors").WithOpenApi();
        group.MapGet("/{id:guid}", GetAuthor).WithName("GetAuthor").WithOpenApi();
        group.MapPost("/", CreateAuthor).WithName("CreateAuthor").WithOpenApi();
        group.MapPut("/{id:guid}", UpdateAuthor).WithName("UpdateAuthor").WithOpenApi();
        group.MapDelete("/{id:guid}", DeleteAuthor).WithName("DeleteAuthor").WithOpenApi();

        return api;
    }

    private static async Task<Ok<AuthorListResponse>> ListAuthors(
        CatalogDbContext db,
        string? search,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Authors
            .AsNoTracking()
            .Where(author => author.DeletedAtUtc == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = Author.NormalizeKey(search);
            query = query.Where(author => author.NormalizedName.Contains(normalizedSearch));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(author => author.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(author => author.ToResponse())
            .ToListAsync();

        return TypedResults.Ok(new AuthorListResponse(items, page, pageSize, total));
    }

    private static async Task<Results<Ok<AuthorResponse>, NotFound>> GetAuthor(CatalogDbContext db, Guid id)
    {
        var author = await db.Authors
            .AsNoTracking()
            .Where(author => author.Id == id && author.DeletedAtUtc == null)
            .Select(author => author.ToResponse())
            .SingleOrDefaultAsync();

        return author is null ? TypedResults.NotFound() : TypedResults.Ok(author);
    }

    private static async Task<Results<Created<AuthorResponse>, ValidationProblem, Conflict<HttpValidationProblemDetails>>> CreateAuthor(
        CatalogDbContext db,
        AuthorRequest request)
    {
        var errors = AuthorFieldValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var normalizedName = Author.NormalizeKey(request.Name!);
        var alreadyExists = await db.Authors.AnyAsync(author =>
            author.DeletedAtUtc == null && author.NormalizedName == normalizedName);

        if (alreadyExists)
        {
            return Conflict("Author already exists.", "An author with this name already exists.");
        }

        var author = Author.Create(request.Name!);
        db.Authors.Add(author);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/v1/authors/{author.Id}", author.ToResponse());
    }

    private static async Task<Results<Ok<AuthorResponse>, NotFound, ValidationProblem, Conflict<HttpValidationProblemDetails>>> UpdateAuthor(
        CatalogDbContext db,
        Guid id,
        AuthorRequest request)
    {
        var errors = AuthorFieldValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var author = await db.Authors.SingleOrDefaultAsync(author => author.Id == id && author.DeletedAtUtc == null);
        if (author is null)
        {
            return TypedResults.NotFound();
        }

        if (author.IsSystem)
        {
            return Conflict("System author cannot be changed.", "The Not Identified author is required by imports and cannot be renamed.");
        }

        var normalizedName = Author.NormalizeKey(request.Name!);
        var alreadyExists = await db.Authors.AnyAsync(other =>
            other.Id != id &&
            other.DeletedAtUtc == null &&
            other.NormalizedName == normalizedName);

        if (alreadyExists)
        {
            return Conflict("Author already exists.", "An author with this name already exists.");
        }

        author.Rename(request.Name!);
        await db.SaveChangesAsync();

        return TypedResults.Ok(author.ToResponse());
    }

    private static async Task<Results<NoContent, NotFound, Conflict<HttpValidationProblemDetails>>> DeleteAuthor(
        CatalogDbContext db,
        Guid id)
    {
        var author = await db.Authors.SingleOrDefaultAsync(author => author.Id == id && author.DeletedAtUtc == null);
        if (author is null)
        {
            return TypedResults.NotFound();
        }

        if (author.IsSystem)
        {
            return Conflict("System author cannot be deleted.", "The Not Identified author is required by imports and cannot be deleted.");
        }

        var hasActiveBooks = await db.Books.AnyAsync(book => book.AuthorId == id && book.DeletedAtUtc == null);
        if (hasActiveBooks)
        {
            return Conflict("Author is in use.", "An author with active books cannot be deleted.");
        }

        author.SoftDelete();
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
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

    private static AuthorResponse ToResponse(this Author author)
        => new(author.Id, author.Name, author.SystemCode, author.IsSystem);
}
