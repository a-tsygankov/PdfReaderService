using Microsoft.EntityFrameworkCore;
using PdfReader.Core;

namespace PdfReader.Api.Data;

public sealed class AppDbContext : DbContext
{
    public DbSet<Document> Documents => Set<Document>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var document = modelBuilder.Entity<Document>();
        document.ToTable("documents");
        document.HasKey(d => d.Id);

        document.Property(d => d.Id)
            .HasColumnName("id");

        document.Property(d => d.OriginalFileName)
            .HasColumnName("original_file_name")
            .IsRequired()
            .HasMaxLength(512);

        document.Property(d => d.ContentType)
            .HasColumnName("content_type")
            .IsRequired()
            .HasMaxLength(256);

        document.Property(d => d.FileSize)
            .HasColumnName("file_size");

        document.Property(d => d.StoragePath)
            .HasColumnName("storage_path")
            .IsRequired()
            .HasMaxLength(1024);

        document.Property(d => d.ResultPath)
            .HasColumnName("result_path")
            .HasMaxLength(1024);

        document.Property(d => d.FormType)
            .HasColumnName("form_type")
            .HasMaxLength(256);

        document.Property(d => d.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(64);

        document.Property(d => d.CreatedAt)
            .HasColumnName("created_at");

        document.Property(d => d.ProcessedAt)
            .HasColumnName("processed_at");

        document.Property(d => d.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(2048);
    }
}
