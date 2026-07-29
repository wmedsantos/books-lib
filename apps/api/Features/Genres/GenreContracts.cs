namespace BooksLib.Api.Features.Genres;

public sealed record GenreListResponse(
    IReadOnlyList<GenreResponse> Items,
    int Page,
    int PageSize,
    int Total);

public sealed record GenreResponse(
    Guid Id,
    string Name,
    string? SystemCode,
    bool IsSystem);

public sealed record GenreRequest(string? Name);
