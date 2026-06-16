using System.ComponentModel.DataAnnotations;

namespace TodoApp.API.DTOs;

// --- Response DTO ---
// We never expose the domain model directly to the client.
// This decouples the API contract from internal storage details.

public record TodoItemResponse(
    int Id,
    string Title,
    string? Description,
    bool IsCompleted,
    string Priority,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// --- Create DTO ---
public record CreateTodoItemRequest(
    [Required, StringLength(200, MinimumLength = 1)]
    string Title,

    string? Description,

    string Priority = "Medium",

    DateTime? DueDate = null
);

// --- Update DTO ---
// Partial update pattern: only supplied fields are changed.
// Using nullable wrappers lets the service distinguish "not provided" from "set to null/false".

public record UpdateTodoItemRequest(
    [StringLength(200, MinimumLength = 1)]
    string? Title,

    string? Description,

    bool? IsCompleted,

    string? Priority,

    DateTime? DueDate
);

// --- Query / filter DTO ---
public record TodoQueryParams(
    bool? IsCompleted = null,
    string? Priority = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20,
    // SortBy: "dueDate" | "createdAt" | "priority" | "title"  (default: dueDate)
    string SortBy = "dueDate",
    // SortDir: "asc" | "desc"  (default: asc — soonest due date first)
    string SortDir = "asc"
);

// --- Paged response wrapper ---
public record PagedResponse<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);
