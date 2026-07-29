using BooksLib.Api.Features.Identity;

namespace BooksLib.Api.Tests.Identity;

public sealed class CatalogUserTests
{
    [Fact]
    public void Create_normalizes_email_for_case_insensitive_lookup()
    {
        var user = CatalogUser.Create("  Admin@BookLib.Local  ");

        Assert.Equal("Admin@BookLib.Local", user.Email);
        Assert.Equal("ADMIN@BOOKLIB.LOCAL", user.NormalizedEmail);
    }

    [Fact]
    public void SetPasswordHash_can_mark_password_change_required()
    {
        var user = CatalogUser.Create("admin@booklib.local");

        user.SetPasswordHash("hash", requirePasswordChange: true);

        Assert.Equal("hash", user.PasswordHash);
        Assert.True(user.PasswordChangeRequired);
    }
}
