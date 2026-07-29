using BooksLib.Api.Logging;
using Microsoft.AspNetCore.Http;

namespace BooksLib.Api.Tests.Logging;

public sealed class RequestLogSanitizerTests
{
    [Fact]
    public void PathOnly_excludes_query_string_values()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/identity/login";
        context.Request.QueryString = new QueryString("?password=DoNotLog123&access_token=secret");

        var path = RequestLogSanitizer.PathOnly(context.Request);

        Assert.Equal("/api/v1/identity/login", path);
        Assert.DoesNotContain("DoNotLog123", path);
        Assert.DoesNotContain("secret", path);
    }

    [Fact]
    public void MessageTemplate_does_not_reference_headers_body_tokens_or_query()
    {
        Assert.DoesNotContain("Authorization", RequestLogSanitizer.MessageTemplate, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", RequestLogSanitizer.MessageTemplate, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Token", RequestLogSanitizer.MessageTemplate, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Query", RequestLogSanitizer.MessageTemplate, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Body", RequestLogSanitizer.MessageTemplate, StringComparison.OrdinalIgnoreCase);
    }
}
