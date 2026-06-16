using TodoApp.API.DTOs;
using TodoApp.API.Models;

namespace TodoApp.API.Repositories;

/// <summary>
/// Repository abstraction. Keeps the service layer independent of EF Core,
/// which makes unit-testing services with mocks straightforward.
/// </summary>
public interface ITodoRepository
{
    Task<(IEnumerable<TodoItem> Items, int TotalCount)> GetAllAsync(TodoQueryParams query, CancellationToken ct = default);
    Task<TodoItem?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TodoItem> CreateAsync(TodoItem item, CancellationToken ct = default);
    Task<TodoItem> UpdateAsync(TodoItem item, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
}
