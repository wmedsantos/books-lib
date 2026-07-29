using BooksLib.Api.Features.Authors;
using BooksLib.Api.Features.Genres;
using Microsoft.EntityFrameworkCore;

namespace BooksLib.Api.Data;

public static class DatabaseBootstrap
{
    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        await db.Database.MigrateAsync();
        await EnsureSystemAuthorAsync(db);
        await EnsureSystemGenreAsync(db);
    }

    private static async Task EnsureSystemAuthorAsync(CatalogDbContext db)
    {
        var exists = await db.Authors.AnyAsync(author => author.SystemCode == Author.SystemCodes.NotIdentified);
        if (exists)
        {
            return;
        }

        db.Authors.Add(Author.CreateSystem("Not Identified", Author.SystemCodes.NotIdentified));
        await db.SaveChangesAsync();
    }

    private static async Task EnsureSystemGenreAsync(CatalogDbContext db)
    {
        var exists = await db.Genres.AnyAsync(genre => genre.SystemCode == Genre.SystemCodes.Unclassified);
        if (exists)
        {
            return;
        }

        db.Genres.Add(Genre.CreateSystem("Unclassified", Genre.SystemCodes.Unclassified));
        await db.SaveChangesAsync();
    }
}
