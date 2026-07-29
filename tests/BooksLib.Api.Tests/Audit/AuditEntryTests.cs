using BooksLib.Api.Features.Audit;

namespace BooksLib.Api.Tests.Audit;

public sealed class AuditEntryTests
{
    [Fact]
    public void Create_records_actor_entity_operation_and_timestamp()
    {
        var entityId = Guid.NewGuid();

        var audit = AuditEntry.Create("admin@booklib.local", "Book", entityId, "SoftDelete");

        Assert.Equal("admin@booklib.local", audit.Actor);
        Assert.Equal("Book", audit.EntityType);
        Assert.Equal(entityId, audit.EntityId);
        Assert.Equal("SoftDelete", audit.Operation);
        Assert.NotEqual(default, audit.OccurredAtUtc);
    }
}
