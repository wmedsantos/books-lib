namespace BooksLib.Api.Features.Authors;

public sealed record AuthorListResponse(
    IReadOnlyList<AuthorResponse> Items,
    int Page,
    int PageSize,
    int Total);

public sealed record AuthorResponse(
    Guid Id,
    string Name,
    string? SystemCode,
    bool IsSystem);

public sealed record AuthorRequest(string? Name);
