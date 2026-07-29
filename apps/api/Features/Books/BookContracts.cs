namespace BooksLib.Api.Features.Books;

public sealed record BookListResponse(
    IReadOnlyList<BookResponse> Items,
    int Page,
    int PageSize,
    int Total);

public sealed record BookResponse(
    Guid Id,
    string Title,
    Guid AuthorId,
    string AuthorName,
    Guid GenreId,
    string GenreName,
    string? CreatorCredit,
    string? Isbn13,
    string? Isbn10,
    string? Description,
    string? Publisher,
    DateOnly? PublishedOn,
    int? PageCount,
    int CopyCount,
    string? CoverUrl,
    string? CollectionName,
    DateOnly? SourceAddedOn,
    bool PublishOnSite);

public sealed record BookRequest(
    string? Title,
    Guid? AuthorId,
    Guid? GenreId,
    string? CreatorCredit,
    string? Isbn13,
    string? Isbn10,
    string? Description,
    string? Publisher,
    DateOnly? PublishedOn,
    int? PageCount,
    int? CopyCount,
    string? CoverUrl,
    string? CollectionName,
    DateOnly? SourceAddedOn,
    bool PublishOnSite);
