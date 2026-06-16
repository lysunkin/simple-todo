using TodoApp.API.DTOs;

namespace TodoApp.API.Services;

/// <summary>
/// Business-logic layer. Controllers call this; the service calls the repository.
/// This indirection keeps validation and mapping out of both the controller and the DB layer.
/// </summary>
public interface ITodoService
{
    Task<PagedResponse<TodoItemResponse>> GetAllAsync(TodoQueryParams query, CancellationToken ct = default);
    Task<TodoItemResponse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TodoItemResponse> CreateAsync(CreateTodoItemRequest request, CancellationToken ct = default);
    Task<TodoItemResponse?> UpdateAsync(int id, UpdateTodoItemRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
