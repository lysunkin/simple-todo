using TodoApp.API.DTOs;
using TodoApp.API.Models;
using TodoApp.API.Repositories;

namespace TodoApp.API.Services;

/// <summary>
/// Orchestrates validation, mapping, and persistence.
/// Trade-off: mapping lives here rather than in AutoMapper to keep dependencies minimal.
/// AutoMapper would be preferable at larger scale.
/// </summary>
public class TodoService : ITodoService
{
    private readonly ITodoRepository _repo;
    private readonly ILogger<TodoService> _logger;

    public TodoService(ITodoRepository repo, ILogger<TodoService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<PagedResponse<TodoItemResponse>> GetAllAsync(
        TodoQueryParams query,
        CancellationToken ct = default)
    {
        var (items, total) = await _repo.GetAllAsync(query, ct);
        return new PagedResponse<TodoItemResponse>(
            Items: items.Select(ToResponse).ToList().AsReadOnly(),
            TotalCount: total,
            Page: query.Page,
            PageSize: query.PageSize
        );
    }

    public async Task<TodoItemResponse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, ct);
        return item is null ? null : ToResponse(item);
    }

    public async Task<TodoItemResponse> CreateAsync(CreateTodoItemRequest request, CancellationToken ct = default)
    {
        var priority = ParsePriority(request.Priority);

        var item = new TodoItem
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Priority = priority,
            DueDate = ToUtc(request.DueDate),
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _repo.CreateAsync(item, ct);
        _logger.LogInformation("Created TodoItem {Id}: {Title}", created.Id, created.Title);
        return ToResponse(created);
    }

    public async Task<TodoItemResponse?> UpdateAsync(
        int id,
        UpdateTodoItemRequest request,
        CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, ct);
        if (item is null) return null;

        // Apply only provided fields (partial update)
        if (request.Title is not null)
            item.Title = request.Title.Trim();

        // Optional<T> distinguishes "absent" (do nothing) from "null" (clear the field)
        if (request.Description.IsPresent)
            item.Description = request.Description.Value?.Trim();

        if (request.IsCompleted.HasValue)
            item.IsCompleted = request.IsCompleted.Value;

        if (request.Priority is not null)
            item.Priority = ParsePriority(request.Priority);

        if (request.DueDate.IsPresent)
            item.DueDate = request.DueDate.Value.HasValue
                ? ToUtc(request.DueDate.Value.Value)
                : null;

        item.UpdatedAt = DateTime.UtcNow;

        var updated = await _repo.UpdateAsync(item, ct);
        _logger.LogInformation("Updated TodoItem {Id}", id);
        return ToResponse(updated);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var deleted = await _repo.DeleteAsync(id, ct);
        if (deleted)
            _logger.LogInformation("Deleted TodoItem {Id}", id);
        return deleted;
    }

    // --- Private helpers ---
    private static TodoItemResponse ToResponse(TodoItem item) => new(
        item.Id,
        item.Title,
        item.Description,
        item.IsCompleted,
        item.Priority.ToString(),
        item.DueDate,
        item.CreatedAt,
        item.UpdatedAt
    );

    /// <summary>
    /// Parses a priority string. Throws <see cref="ArgumentException"/> for unknown values
    /// so the caller can return a 400 Bad Request rather than silently coercing to Medium.
    /// </summary>
    private static Priority ParsePriority(string? value)
    {
        if (Enum.TryParse<Priority>(value, ignoreCase: true, out var p))
            return p;

        var valid = string.Join(", ", Enum.GetNames<Priority>());
        throw new ArgumentException($"Invalid priority '{value}'. Valid values: {valid}.");
    }

    /// <summary>
    /// Ensures a DateTime has Kind=Utc before it reaches the PostgreSQL driver.
    /// JSON deserialization produces Kind=Unspecified for ISO strings without a
    /// timezone suffix, which Npgsql rejects for 'timestamp with time zone' columns.
    /// </summary>
    private static DateTime? ToUtc(DateTime? dt) =>
        dt.HasValue ? ToUtc(dt.Value) : null;

    private static DateTime ToUtc(DateTime dt) =>
        dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
}
