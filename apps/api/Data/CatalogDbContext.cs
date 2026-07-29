using BooksLib.Api.Features.Audit;
using BooksLib.Api.Features.Authors;
using BooksLib.Api.Features.Books;
using BooksLib.Api.Features.Genres;
using BooksLib.Api.Features.Identity;
using Microsoft.EntityFrameworkCore;

namespace BooksLib.Api.Data;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<CatalogUser> Users => Set<CatalogUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AuditEntryConfiguration());
        modelBuilder.ApplyConfiguration(new AuthorConfiguration());
        modelBuilder.ApplyConfiguration(new BookConfiguration());
        modelBuilder.ApplyConfiguration(new GenreConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogUserConfiguration());
    }
}
