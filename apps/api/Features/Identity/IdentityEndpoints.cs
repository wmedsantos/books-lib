using System.Security.Claims;
using BooksLib.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BooksLib.Api.Features.Identity;

public static class IdentityEndpoints
{
    public static RouteGroupBuilder MapIdentity(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/identity").WithTags("Identity");

        group.MapPost("/login", Login).AllowAnonymous().WithName("Login").WithOpenApi();
        group.MapPost("/change-password", ChangePassword).RequireAuthorization().WithName("ChangePassword").WithOpenApi();

        return api;
    }

    private static async Task<Results<Ok<LoginResponse>, ValidationProblem, UnauthorizedHttpResult>> Login(
        CatalogDbContext db,
        IPasswordHasher<CatalogUser> passwordHasher,
        TokenService tokenService,
        LoginRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors["email"] = ["Email is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors["password"] = ["Password is required."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var normalizedEmail = CatalogUser.NormalizeEmail(request.Email!);
        var user = await db.Users.SingleOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password!);
        if (verification == PasswordVerificationResult.Failed)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(new LoginResponse(tokenService.CreateToken(user), user.Email, user.PasswordChangeRequired));
    }

    private static async Task<Results<Ok<LoginResponse>, ValidationProblem, UnauthorizedHttpResult>> ChangePassword(
        ClaimsPrincipal principal,
        CatalogDbContext db,
        IPasswordHasher<CatalogUser> passwordHasher,
        TokenService tokenService,
        ChangePasswordRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            errors["currentPassword"] = ["Current password is required."];
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 10)
        {
            errors["newPassword"] = ["New password must be at least 10 characters."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return TypedResults.Unauthorized();
        }

        var user = await db.Users.SingleOrDefaultAsync(user => user.Id == userId);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword!);
        if (verification == PasswordVerificationResult.Failed)
        {
            return TypedResults.Unauthorized();
        }

        user.SetPasswordHash(passwordHasher.HashPassword(user, request.NewPassword!), requirePasswordChange: false);
        await db.SaveChangesAsync();

        return TypedResults.Ok(new LoginResponse(tokenService.CreateToken(user), user.Email, user.PasswordChangeRequired));
    }
}
