namespace BooksLib.Api.Features.Identity;

public sealed record LoginRequest(string? Email, string? Password);

public sealed record LoginResponse(string AccessToken, string Email, bool PasswordChangeRequired);

public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);
