using Microsoft.AspNetCore.Http;

namespace BooksLib.Api.Logging;

public static class RequestLogSanitizer
{
    public const string MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

    public static string PathOnly(HttpRequest request)
        => request.Path.HasValue ? request.Path.Value! : "/";
}
