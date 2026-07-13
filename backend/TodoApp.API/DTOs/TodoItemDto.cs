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

    // Priority must be one of the named enum values. Validated in the service.
    string Priority = "Medium",

    DateTime? DueDate = null
);

// --- Update DTO ---
// Partial update pattern: only supplied fields are changed.
//
// Problem with plain nullable types: for reference types (string?) and value types
// wrapped in Nullable<T> (DateTime?), there is no way to distinguish
// "field was absent from the request" from "field was explicitly set to null".
// Both arrive as C# null, so a client cannot clear a previously-set Description
// or DueDate back to null.
//
// Solution: wrap optional-nullable fields in an Optional<T> struct.
//   - Field absent from JSON  → Optional<T>.IsPresent == false  (do nothing)
//   - Field present as null   → Optional<T>.IsPresent == true, Value == null  (clear it)
//   - Field present with value→ Optional<T>.IsPresent == true, Value == <value>  (set it)
//
// Non-nullable fields (Title, IsCompleted, Priority) keep plain nullable wrappers
// because "set to null" is not a valid operation for them.

public record UpdateTodoItemRequest(
    [StringLength(200, MinimumLength = 1)]
    string? Title,

    Optional<string?> Description,

    bool? IsCompleted,

    string? Priority,

    Optional<DateTime?> DueDate
);

/// <summary>
/// Discriminates between "field absent from the request" and "field explicitly set to null".
/// Used on PATCH fields that are nullable in the domain model so clients can clear them.
/// </summary>
/// <remarks>
/// System.Text.Json does not support this out of the box for record positional constructors,
/// so the converter <see cref="OptionalJsonConverterFactory"/> handles serialisation.
/// </remarks>
public readonly struct Optional<T>
{
    public bool IsPresent { get; }
    public T Value { get; }

    public Optional(T value)
    {
        IsPresent = true;
        Value = value;
    }

    /// <summary>Returns an Optional that signals the field was absent.</summary>
    public static Optional<T> Absent() => default;

    /// <summary>
    /// Allows natural assignment syntax: <c>Optional&lt;string?&gt; x = "hello";</c>
    /// or <c>Optional&lt;string?&gt; x = null;</c> (present-with-null).
    /// To represent an absent field use <see cref="Absent"/> or the struct default.
    /// </summary>
    public static implicit operator Optional<T>(T value) => new(value);
}

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
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);
