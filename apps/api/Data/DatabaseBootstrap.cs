using BooksLib.Api.Features.Identity;
using BooksLib.Api.Features.Authors;
using BooksLib.Api.Features.Genres;
using Microsoft.AspNetCore.Identity;
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
        await EnsureBootstrapUserAsync(scope.ServiceProvider, db, app.Configuration);
    }

    private static async Task EnsureBootstrapUserAsync(
        IServiceProvider services,
        CatalogDbContext db,
        IConfiguration configuration)
    {
        var email = configuration["Bootstrap:Email"];
        var password = configuration["Bootstrap:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var normalizedEmail = CatalogUser.NormalizeEmail(email);
        var exists = await db.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail);
        if (exists)
        {
            return;
        }

        var user = CatalogUser.Create(email);
        var passwordHasher = services.GetRequiredService<IPasswordHasher<CatalogUser>>();
        user.SetPasswordHash(passwordHasher.HashPassword(user, password), requirePasswordChange: true);

        db.Users.Add(user);
        await db.SaveChangesAsync();
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
