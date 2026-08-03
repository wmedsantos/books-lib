using Microsoft.Extensions.Configuration;

namespace BooksLib.Api.Data;

public static class DatabaseConnectionString
{
    public static string Resolve(IConfiguration configuration)
    {
        var databaseUrl = configuration["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return FromPostgresUrl(databaseUrl);
        }

        var configured = configuration.GetConnectionString("Catalog");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        throw new InvalidOperationException("DATABASE_URL or ConnectionStrings:Catalog must be configured.");
    }

    public static string FromPostgresUrl(string databaseUrl)
    {
        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            return databaseUrl;
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));

        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = database,
            Username = username,
            Password = password
        };

        var sslMode = GetQueryValue(uri.Query, "sslmode");
        if (sslMode is not null && sslMode.Equals("require", StringComparison.OrdinalIgnoreCase))
        {
            builder.SslMode = Npgsql.SslMode.Require;
        }

        return builder.ConnectionString;
    }

    private static string? GetQueryValue(string query, string key)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (Uri.UnescapeDataString(parts[0]).Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "";
            }
        }

        return null;
    }
}
