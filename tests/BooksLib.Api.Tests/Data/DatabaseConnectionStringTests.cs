using BooksLib.Api.Data;
using Microsoft.Extensions.Configuration;

namespace BooksLib.Api.Tests.Data;

public sealed class DatabaseConnectionStringTests
{
    [Fact]
    public void Resolve_prefers_database_url_over_catalog_connection_string()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DATABASE_URL"] = "postgresql://bookslib:secret@database.internal:5432/bookslib",
                ["ConnectionStrings:Catalog"] = "Host=localhost;Database=local"
            })
            .Build();

        var connectionString = DatabaseConnectionString.Resolve(configuration);

        Assert.Contains("Host=database.internal", connectionString);
        Assert.Contains("Database=bookslib", connectionString);
        Assert.DoesNotContain("localhost", connectionString);
    }

    [Fact]
    public void FromPostgresUrl_decodes_credentials_and_database_name()
    {
        var connectionString = DatabaseConnectionString.FromPostgresUrl(
            "postgresql://book%20user:p%40ssword@db.example.com:5433/books%20prod");

        Assert.Contains("Host=db.example.com", connectionString);
        Assert.Contains("Port=5433", connectionString);
        Assert.Contains("Database=\"books prod\"", connectionString);
        Assert.Contains("Username=\"book user\"", connectionString);
        Assert.Contains("Password=p@ssword", connectionString);
    }

    [Fact]
    public void FromPostgresUrl_preserves_non_url_connection_strings()
    {
        var connectionString = "Host=localhost;Database=bookslib";

        Assert.Equal(connectionString, DatabaseConnectionString.FromPostgresUrl(connectionString));
    }
}
