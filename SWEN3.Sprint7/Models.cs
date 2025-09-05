using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SWEN3.Sprint7;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ConfigurationSectionAttribute : Attribute
{
    public ConfigurationSectionAttribute(string path)
    {
        Path = path;
    }

    public string Path { get; }
}

public sealed class DailyDocumentAccess
{
    [Key] public Guid Id { get; set; }
    [Required] public Guid DocumentId { get; set; }
    [Required] public DateOnly LogDate { get; set; }
    [Required] public int AccessCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed record Document
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string StoragePath { get; init; } = string.Empty;
}

public sealed class BatchDbContext : DbContext
{
    public BatchDbContext(DbContextOptions<BatchDbContext> options) : base(options)
    {
    }

    public DbSet<DailyDocumentAccess> DailyDocumentAccesses => Set<DailyDocumentAccess>();
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DailyDocumentAccess>(e =>
        {
            e.ToTable("daily_document_access");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.DocumentId).HasColumnName("document_id").IsRequired();
            e.Property(x => x.LogDate).HasColumnName("log_date").IsRequired();
            e.Property(x => x.AccessCount).HasColumnName("access_count").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasIndex(x => new { x.DocumentId, x.LogDate }).IsUnique()
                .HasDatabaseName("ix_daily_document_access_document_date");
            e.HasIndex(x => x.LogDate).HasDatabaseName("ix_daily_document_access_log_date");
        });

        modelBuilder.Entity<Document>(e =>
        {
            e.ToTable("documents");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
            e.Property(x => x.StoragePath).HasColumnName("storage_path").HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<DailyDocumentAccess>().HasOne<Document>().WithMany().HasForeignKey(d => d.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}