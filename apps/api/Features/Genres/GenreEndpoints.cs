using System.Security.Claims;
using BooksLib.Api.Data;
using BooksLib.Api.Features.Audit;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace BooksLib.Api.Features.Genres;

public static class GenreEndpoints
{
    public static RouteGroupBuilder MapGenres(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/genres").WithTags("Genres");

        group.MapGet("/", ListGenres)
            .WithName("ListGenres")
            .WithOpenApi();

        group.MapGet("/{id:guid}", GetGenre)
            .WithName("GetGenre")
            .WithOpenApi();

        group.MapPost("/", CreateGenre)
            .RequireAuthorization("CatalogWrite")
            .WithName("CreateGenre")
            .WithOpenApi();

        group.MapPut("/{id:guid}", UpdateGenre)
            .RequireAuthorization("CatalogWrite")
            .WithName("UpdateGenre")
            .WithOpenApi();

        group.MapDelete("/{id:guid}", DeleteGenre)
            .RequireAuthorization("CatalogWrite")
            .WithName("DeleteGenre")
            .WithOpenApi();

        return api;
    }

    private static async Task<Ok<GenreListResponse>> ListGenres(
        CatalogDbContext db,
        string? search,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Genres
            .AsNoTracking()
            .Where(genre => genre.DeletedAtUtc == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = Genre.NormalizeKey(search);
            query = query.Where(genre => genre.NormalizedName.Contains(normalizedSearch));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(genre => genre.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(genre => genre.ToResponse())
            .ToListAsync();

        return TypedResults.Ok(new GenreListResponse(items, page, pageSize, total));
    }

    private static async Task<Results<Ok<GenreResponse>, NotFound>> GetGenre(CatalogDbContext db, Guid id)
    {
        var genre = await db.Genres
            .AsNoTracking()
            .Where(genre => genre.Id == id && genre.DeletedAtUtc == null)
            .Select(genre => genre.ToResponse())
            .SingleOrDefaultAsync();

        return genre is null ? TypedResults.NotFound() : TypedResults.Ok(genre);
    }

    private static async Task<Results<Created<GenreResponse>, ValidationProblem, Conflict<HttpValidationProblemDetails>>> CreateGenre(
        CatalogDbContext db,
        GenreRequest request)
    {
        var errors = GenreFieldValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var normalizedName = Genre.NormalizeKey(request.Name!);
        var alreadyExists = await db.Genres.AnyAsync(genre =>
            genre.DeletedAtUtc == null && genre.NormalizedName == normalizedName);

        if (alreadyExists)
        {
            return Conflict("Genre already exists.", "A genre with this name already exists.");
        }

        var genre = Genre.Create(request.Name!);
        db.Genres.Add(genre);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/v1/genres/{genre.Id}", genre.ToResponse());
    }

    private static async Task<Results<Ok<GenreResponse>, NotFound, ValidationProblem, Conflict<HttpValidationProblemDetails>>> UpdateGenre(
        CatalogDbContext db,
        Guid id,
        GenreRequest request)
    {
        var errors = GenreFieldValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var genre = await db.Genres.SingleOrDefaultAsync(genre => genre.Id == id && genre.DeletedAtUtc == null);
        if (genre is null)
        {
            return TypedResults.NotFound();
        }

        if (genre.IsSystem)
        {
            return Conflict("System genre cannot be changed.", "The Unclassified genre is required by imports and cannot be renamed.");
        }

        var normalizedName = Genre.NormalizeKey(request.Name!);
        var alreadyExists = await db.Genres.AnyAsync(other =>
            other.Id != id &&
            other.DeletedAtUtc == null &&
            other.NormalizedName == normalizedName);

        if (alreadyExists)
        {
            return Conflict("Genre already exists.", "A genre with this name already exists.");
        }

        genre.Rename(request.Name!);
        await db.SaveChangesAsync();

        return TypedResults.Ok(genre.ToResponse());
    }

    private static async Task<Results<NoContent, NotFound, Conflict<HttpValidationProblemDetails>>> DeleteGenre(
        CatalogDbContext db,
        ClaimsPrincipal principal,
        Guid id)
    {
        var genre = await db.Genres.SingleOrDefaultAsync(genre => genre.Id == id && genre.DeletedAtUtc == null);
        if (genre is null)
        {
            return TypedResults.NotFound();
        }

        if (genre.IsSystem)
        {
            return Conflict("System genre cannot be deleted.", "The Unclassified genre is required by imports and cannot be deleted.");
        }

        var hasActiveBooks = await db.Books.AnyAsync(book => book.GenreId == id && book.DeletedAtUtc == null);
        if (hasActiveBooks)
        {
            return Conflict("Genre is in use.", "A genre with active books cannot be deleted.");
        }

        genre.SoftDelete();
        db.AuditEntries.Add(AuditEntry.Create(principal.Identity?.Name ?? "unknown", "Genre", id, "SoftDelete"));
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

    private static GenreResponse ToResponse(this Genre genre)
        => new(genre.Id, genre.Name, genre.SystemCode, genre.IsSystem);
}
