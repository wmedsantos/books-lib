using BooksLib.Api.Features.Genres;
using Microsoft.EntityFrameworkCore;

namespace BooksLib.Api.Data;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Genre> Genres => Set<Genre>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new GenreConfiguration());
    }
}
