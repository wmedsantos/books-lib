using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BooksLib.Api.Data;
using BooksLib.Api.Features.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace BooksLib.Api.Tests.Identity;

public sealed class AuthenticationIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Login_rejects_invalid_credentials()
    {
        await using var factory = new TestApiFactory();
        await factory.SeedUserAsync("admin@bookslib.local", "CorrectPassword123!", requirePasswordChange: false);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/identity/login", new
        {
            email = "admin@bookslib.local",
            password = "WrongPassword123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Catalog_write_without_token_returns_unauthorized()
    {
        await using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/authors", new { name = "Conceicao Evaristo" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Catalog_write_with_expired_credential_token_returns_forbidden()
    {
        await using var factory = new TestApiFactory();
        await factory.SeedUserAsync("admin@bookslib.local", "ChangeMe123!", requirePasswordChange: true);
        var client = factory.CreateClient();
        var login = await LoginAsync(client, "admin@bookslib.local", "ChangeMe123!");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var response = await client.PostAsJsonAsync("/api/v1/authors", new { name = "Conceicao Evaristo" });

        Assert.True(login.PasswordChangeRequired);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Change_password_returns_active_token_that_can_write_catalog()
    {
        await using var factory = new TestApiFactory();
        await factory.SeedUserAsync("admin@bookslib.local", "ChangeMe123!", requirePasswordChange: true);
        var client = factory.CreateClient();
        var login = await LoginAsync(client, "admin@bookslib.local", "ChangeMe123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var changePasswordResponse = await client.PostAsJsonAsync("/api/v1/identity/change-password", new
        {
            currentPassword = "ChangeMe123!",
            newPassword = "ChangedPassword123!"
        });
        changePasswordResponse.EnsureSuccessStatusCode();
        var changedLogin = await changePasswordResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", changedLogin!.AccessToken);
        var writeResponse = await client.PostAsJsonAsync("/api/v1/authors", new { name = "Conceicao Evaristo" });

        Assert.False(changedLogin.PasswordChangeRequired);
        Assert.Equal(HttpStatusCode.Created, writeResponse.StatusCode);
    }

    [Fact]
    public async Task Catalog_write_with_expired_jwt_returns_unauthorized()
    {
        await using var factory = new TestApiFactory(jwtExpirationMinutes: -5);
        await factory.SeedUserAsync("admin@bookslib.local", "CorrectPassword123!", requirePasswordChange: false);
        var client = factory.CreateClient();
        var login = await LoginAsync(client, "admin@bookslib.local", "CorrectPassword123!");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var response = await client.PostAsJsonAsync("/api/v1/authors", new { name = "Conceicao Evaristo" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<LoginResponse> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/identity/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions))!;
    }
}

internal sealed class TestApiFactory(int jwtExpirationMinutes = 60) : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"BooksLibTests-{Guid.NewGuid()}";
    private readonly int jwtExpirationMinutes = jwtExpirationMinutes;

    public TestApiFactory()
        : this(60)
    {
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("Jwt__Issuer", "BooksLib.Tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "BooksLib.Tests.Web");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "test-signing-key-for-auth-integration-tests-12345");
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", jwtExpirationMinutes.ToString());
        Environment.SetEnvironmentVariable("Database__AutoMigrate", "false");

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CatalogDbContext>>();
            services.AddDbContext<CatalogDbContext>(options => options.UseInMemoryDatabase(databaseName));
        });
    }

    public async Task SeedUserAsync(string email, string password, bool requirePasswordChange)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<CatalogUser>>();
        var user = CatalogUser.Create(email);
        user.SetPasswordHash(passwordHasher.HashPassword(user, password), requirePasswordChange);

        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
}
