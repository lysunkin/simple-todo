using Microsoft.EntityFrameworkCore;
using TodoApp.API.Models;

namespace TodoApp.API.Data;

/// <summary>
/// EF Core context. Supports both SQLite (dev/tests) and PostgreSQL (production).
/// The provider is selected at startup via configuration — no code changes needed
/// between environments.
/// </summary>
public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Explicit lowercase table name avoids PostgreSQL case-sensitivity issues.
            // Without this, EF quotes the name as "TodoItems" in SQL — if the table was
            // created unquoted (e.g. by a prior broken migration) Postgres stores it as
            // "todoitems" and the quoted lookup fails with "relation does not exist".
            entity.ToTable("todo_items");

            entity.Property(e => e.Title)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.Description)
                  .HasMaxLength(2000);

            // Store enums as integers for efficiency; trade-off: less readable in raw SQL.
            // Alternative: store as strings for readability at cost of storage.
            entity.Property(e => e.Priority)
                  .HasConversion<int>();

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Index for the most common filter: incomplete tasks by priority
            entity.HasIndex(e => new { e.IsCompleted, e.Priority });

            // Index for due date sorting/filtering
            entity.HasIndex(e => e.DueDate);
        });

        base.OnModelCreating(modelBuilder);
    }
}
