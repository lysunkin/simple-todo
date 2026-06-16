using Microsoft.AspNetCore.Mvc;
using TodoApp.API.DTOs;
using TodoApp.API.Services;

namespace TodoApp.API.Controllers;

/// <summary>
/// REST controller for to-do items.
/// Thin layer: receives HTTP, delegates to service, returns HTTP.
/// No business logic lives here.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TodoController : ControllerBase
{
    private readonly ITodoService _service;

    public TodoController(ITodoService service)
    {
        _service = service;
    }

    /// <summary>Get a paged, filtered list of to-do items.</summary>
    /// <remarks>
    /// Supports filtering by completion status, priority, and free-text search.
    /// Default sort: dueDate desc (most recent due date first, nulls last).
    /// sortBy: dueDate | createdAt | priority | title
    /// sortDir: asc | desc
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<TodoItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? isCompleted,
        [FromQuery] string? priority,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "dueDate",
        [FromQuery] string sortDir = "asc",
        CancellationToken ct = default)
    {
        var query = new TodoQueryParams(isCompleted, priority, search, page, pageSize, sortBy, sortDir);
        var result = await _service.GetAllAsync(query, ct);
        return Ok(result);
    }

    /// <summary>Get a single to-do item by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TodoItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var item = await _service.GetByIdAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Create a new to-do item.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TodoItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTodoItemRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Partially update a to-do item. Only supplied fields are changed.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TodoItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateTodoItemRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var updated = await _service.UpdateAsync(id, request, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Delete a to-do item.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
