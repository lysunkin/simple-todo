using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TodoApp.API.DTOs;
using TodoApp.API.Models;
using TodoApp.API.Repositories;
using TodoApp.API.Services;
using Xunit;

namespace TodoApp.Tests.Services;

/// <summary>
/// Unit tests for TodoService.
/// The repository is mocked so tests are fast and have no I/O dependency.
/// SQLite in-memory is used in repository integration tests instead.
/// </summary>
public class TodoServiceTests
{
    private readonly Mock<ITodoRepository> _repoMock;
    private readonly TodoService _sut;

    public TodoServiceTests()
    {
        _repoMock = new Mock<ITodoRepository>();
        _sut = new TodoService(_repoMock.Object, NullLogger<TodoService>.Instance);
    }

    // --- GetAll ---
    [Fact]
    public async Task GetAllAsync_ReturnsMappedPagedResponse()
    {
        var items = new List<TodoItem>
        {
            new() { Id = 1, Title = "Buy milk", Priority = Priority.Low, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Id = 2, Title = "Ship feature", Priority = Priority.High, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };
        _repoMock
            .Setup(r => r.GetAllAsync(It.IsAny<TodoQueryParams>(), default))
            .ReturnsAsync((items, 2));

        var result = await _sut.GetAllAsync(new TodoQueryParams());

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.First().Title.Should().Be("Buy milk");
    }

    // --- GetById ---
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((TodoItem?)null);

        var result = await _sut.GetByIdAsync(99);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedItem_WhenFound()
    {
        var item = new TodoItem { Id = 1, Title = "Test", Priority = Priority.Medium, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(item);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Title.Should().Be("Test");
        result.Priority.Should().Be("Medium");
    }

    // --- Create ---
    [Fact]
    public async Task CreateAsync_SetsDefaultsAndReturnsDto()
    {
        var request = new CreateTodoItemRequest("Buy groceries", null, "High", null);
        _repoMock
            .Setup(r => r.CreateAsync(It.IsAny<TodoItem>(), default))
            .ReturnsAsync((TodoItem item, CancellationToken _) => { item.Id = 42; return item; });

        var result = await _sut.CreateAsync(request);

        result.Id.Should().Be(42);
        result.Title.Should().Be("Buy groceries");
        result.Priority.Should().Be("High");
        result.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_TrimsWhitespace()
    {
        var request = new CreateTodoItemRequest("  Trimmed  ", "  desc  ", "Low", null);
        TodoItem? captured = null;
        _repoMock
            .Setup(r => r.CreateAsync(It.IsAny<TodoItem>(), default))
            .Callback<TodoItem, CancellationToken>((item, _) => captured = item)
            .ReturnsAsync((TodoItem item, CancellationToken _) => { item.Id = 1; return item; });

        await _sut.CreateAsync(request);

        captured!.Title.Should().Be("Trimmed");
        captured.Description.Should().Be("desc");
    }

    [Fact]
    public async Task CreateAsync_Throws_ForUnknownPriorityValue()
    {
        var request = new CreateTodoItemRequest("Test", null, "Bogus", null);

        await _sut.Invoking(s => s.CreateAsync(request))
                  .Should().ThrowAsync<ArgumentException>()
                  .WithMessage("*Bogus*");
    }

    // --- Update ---
    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenItemNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((TodoItem?)null);

        var result = await _sut.UpdateAsync(99, new UpdateTodoItemRequest(null, default, null, null, default));

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_AppliesPartialUpdate()
    {
        var existing = new TodoItem
        {
            Id = 1, Title = "Old title", IsCompleted = false,
            Priority = Priority.Low, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<TodoItem>(), default))
                 .ReturnsAsync((TodoItem item, CancellationToken _) => item);

        var result = await _sut.UpdateAsync(1, new UpdateTodoItemRequest("New title", default, true, null, default));

        result!.Title.Should().Be("New title");
        result.IsCompleted.Should().BeTrue();
        result.Priority.Should().Be("Low"); // unchanged
    }

    // --- Delete ---
    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenDeleted()
    {
        _repoMock.Setup(r => r.DeleteAsync(1, default)).ReturnsAsync(true);

        var result = await _sut.DeleteAsync(1);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        _repoMock.Setup(r => r.DeleteAsync(99, default)).ReturnsAsync(false);

        var result = await _sut.DeleteAsync(99);

        result.Should().BeFalse();
    }
}
