namespace TodoApp.API.Models;

/// <summary>
/// Core domain entity for a to-do task.
/// Kept intentionally simple — no soft-delete, no assignee, no tags.
/// Those would be natural next steps (see README).
/// </summary>
public class TodoItem
{
    public int Id { get; set; }

    /// <summary>Short summary of the task (required).</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional longer description.</summary>
    public string? Description { get; set; }

    /// <summary>Whether the task has been completed.</summary>
    public bool IsCompleted { get; set; }

    /// <summary>Priority level: Low = 0, Medium = 1, High = 2.</summary>
    public Priority Priority { get; set; } = Priority.Medium;

    /// <summary>Optional due date (UTC). No enforcement at DB layer — validation is in the service.</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>UTC timestamp when the record was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of the last update. Updated by the service layer.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum Priority
{
    Low = 0,
    Medium = 1,
    High = 2
}
