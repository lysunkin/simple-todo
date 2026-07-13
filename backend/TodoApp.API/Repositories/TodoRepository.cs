using Microsoft.EntityFrameworkCore;
using TodoApp.API.Data;
using TodoApp.API.DTOs;
using TodoApp.API.Models;

namespace TodoApp.API.Repositories;

/// <summary>
/// EF Core implementation of the repository.
/// All database I/O is async and honours cancellation tokens for proper
/// request-cancellation support under load.
/// </summary>
public class TodoRepository : ITodoRepository
{
    private readonly TodoDbContext _db;

    public TodoRepository(TodoDbContext db)
    {
        _db = db;
    }

    public async Task<(IEnumerable<TodoItem> Items, int TotalCount)> GetAllAsync(
        TodoQueryParams query,
        CancellationToken ct = default)
    {
        var q = _db.TodoItems.AsQueryable();

        // --- Filters ---
        if (query.IsCompleted.HasValue)
            q = q.Where(t => t.IsCompleted == query.IsCompleted.Value);

        if (!string.IsNullOrWhiteSpace(query.Priority) &&
            Enum.TryParse<Priority>(query.Priority, ignoreCase: true, out var priority))
        {
            q = q.Where(t => t.Priority == priority);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.ToLower();
            q = q.Where(t => t.Title.ToLower().Contains(term) ||
                              (t.Description != null && t.Description.ToLower().Contains(term)));
        }

        // --- Count before pagination ---
        var total = await q.CountAsync(ct);

        // --- Ordering ---
        // Default: due date ascending (soonest due date first — today before tomorrow),
        // with NULL due dates pushed to the bottom so tasks without a due date don't
        // crowd out tasks that are actually due soon.
        var desc = !query.SortDir.Equals("asc", StringComparison.OrdinalIgnoreCase);

        q = query.SortBy.ToLowerInvariant() switch
        {
            "createdat" => desc
                ? q.OrderByDescending(t => t.CreatedAt)
                : q.OrderBy(t => t.CreatedAt),

            "priority" => desc
                ? q.OrderByDescending(t => t.Priority)
                : q.OrderBy(t => t.Priority),

            "title" => desc
                ? q.OrderByDescending(t => t.Title)
                : q.OrderBy(t => t.Title),

            // "duedate" is the default. NULLs last regardless of direction:
            // ascending  -> earliest due date first, NULLs at the bottom
            // descending -> latest due date first,   NULLs at the bottom
            _ => desc
                ? q.OrderByDescending(t => t.DueDate.HasValue)   // true (has date) sorts before false
                     .ThenByDescending(t => t.DueDate)
                : q.OrderByDescending(t => t.DueDate.HasValue)
                     .ThenBy(t => t.DueDate),
        };

        // --- Pagination ---
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(query.Page, 1);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<TodoItem?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _db.TodoItems.FindAsync(new object[] { id }, ct);

    public async Task<TodoItem> CreateAsync(TodoItem item, CancellationToken ct = default)
    {
        _db.TodoItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return item;
    }

    public async Task<TodoItem> UpdateAsync(TodoItem item, CancellationToken ct = default)
    {
        _db.TodoItems.Update(item);
        await _db.SaveChangesAsync(ct);
        return item;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var item = await _db.TodoItems.FindAsync(new object[] { id }, ct);
        if (item is null) return false;

        _db.TodoItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
