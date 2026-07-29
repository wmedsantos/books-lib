using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BooksLib.Api.Features.Audit;

public sealed class AuditEntry
{
    private AuditEntry()
    {
    }

    public Guid Id { get; private set; }
    public string Actor { get; private set; } = "";
    public string EntityType { get; private set; } = "";
    public Guid EntityId { get; private set; }
    public string Operation { get; private set; } = "";
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static AuditEntry Create(string actor, string entityType, Guid entityId, string operation)
    {
        return new AuditEntry
        {
            Id = Guid.NewGuid(),
            Actor = actor,
            EntityType = entityType,
            EntityId = entityId,
            Operation = operation,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
    }
}

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");
        builder.HasKey(audit => audit.Id);

        builder.Property(audit => audit.Id).HasColumnName("id");
        builder.Property(audit => audit.Actor).HasColumnName("actor").HasMaxLength(254).IsRequired();
        builder.Property(audit => audit.EntityType).HasColumnName("entity_type").HasMaxLength(80).IsRequired();
        builder.Property(audit => audit.EntityId).HasColumnName("entity_id").IsRequired();
        builder.Property(audit => audit.Operation).HasColumnName("operation").HasMaxLength(80).IsRequired();
        builder.Property(audit => audit.OccurredAtUtc).HasColumnName("occurred_at_utc").IsRequired();
    }
}
