using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TodoApp.API.Data;
using TodoApp.API.DTOs;
using TodoApp.API.Models;
using TodoApp.API.Repositories;
using Xunit;

namespace TodoApp.Tests.Repositories;

/// <summary>
/// Integration tests for TodoRepository using SQLite in-memory via a shared connection.
/// Using a real SQLite database (rather than EF InMemory) validates actual SQL behaviour,
/// index usage, and constraints — things EF InMemory silently ignores.
///
/// Trade-off: slightly more setup overhead vs. EF InMemory, but much higher fidelity.
/// </summary>
public class TodoRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TodoDbContext _db;
    private readonly TodoRepository _sut;

    public TodoRepositoryTests()
    {
        // Keep the connection open for the lifetime of the test so the in-memory
        // SQLite database is not destroyed between operations.
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TodoDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TodoDbContext(options);
        _db.Database.EnsureCreated();

        _sut = new TodoRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    // --- Create ---
    [Fact]
    public async Task CreateAsync_PersistsItem()
    {
        var item = MakeItem("Write tests");

        var created = await _sut.CreateAsync(item);

        created.Id.Should().BeGreaterThan(0);
        var fromDb = await _db.TodoItems.FindAsync(created.Id);
        fromDb.Should().NotBeNull();
        fromDb!.Title.Should().Be("Write tests");
    }

    // --- GetById ---
    [Fact]
    public async Task GetByIdAsync_ReturnsItem_WhenExists()
    {
        var item = await _sut.CreateAsync(MakeItem("Read docs"));

        var result = await _sut.GetByIdAsync(item.Id);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Read docs");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenMissing()
    {
        var result = await _sut.GetByIdAsync(9999);
        result.Should().BeNull();
    }

    // --- GetAll / filters ---
    [Fact]
    public async Task GetAllAsync_FiltersByCompletion()
    {
        await _sut.CreateAsync(MakeItem("Task A", isCompleted: false));
        await _sut.CreateAsync(MakeItem("Task B", isCompleted: true));
        await _sut.CreateAsync(MakeItem("Task C", isCompleted: false));

        var (items, total) = await _sut.GetAllAsync(new TodoQueryParams(IsCompleted: false));

        total.Should().Be(2);
        items.Should().OnlyContain(i => !i.IsCompleted);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByPriority()
    {
        await _sut.CreateAsync(MakeItem("High task", priority: Priority.High));
        await _sut.CreateAsync(MakeItem("Low task", priority: Priority.Low));

        var (items, total) = await _sut.GetAllAsync(new TodoQueryParams(Priority: "High"));

        total.Should().Be(1);
        items.First().Title.Should().Be("High task");
    }

    [Fact]
    public async Task GetAllAsync_SearchesTitleAndDescription()
    {
        await _sut.CreateAsync(MakeItem("Deploy service", description: "Kubernetes"));
        await _sut.CreateAsync(MakeItem("Write unit tests", description: "xUnit framework"));

        var (items, _) = await _sut.GetAllAsync(new TodoQueryParams(Search: "unit"));

        items.Should().HaveCount(1);
        items.First().Title.Should().Be("Write unit tests");
    }

    [Fact]
    public async Task GetAllAsync_PaginatesCorrectly()
    {
        for (int i = 1; i <= 5; i++)
            await _sut.CreateAsync(MakeItem($"Task {i}"));

        var (items, total) = await _sut.GetAllAsync(new TodoQueryParams(Page: 2, PageSize: 2));

        total.Should().Be(5);
        items.Should().HaveCount(2);
    }

    // --- Update ---
    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var item = await _sut.CreateAsync(MakeItem("Original"));
        item.Title = "Updated";
        item.IsCompleted = true;

        await _sut.UpdateAsync(item);

        var fromDb = await _sut.GetByIdAsync(item.Id);
        fromDb!.Title.Should().Be("Updated");
        fromDb.IsCompleted.Should().BeTrue();
    }

    // --- Delete ---
    [Fact]
    public async Task DeleteAsync_RemovesItem()
    {
        var item = await _sut.CreateAsync(MakeItem("To delete"));

        var result = await _sut.DeleteAsync(item.Id);

        result.Should().BeTrue();
        (await _sut.GetByIdAsync(item.Id)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _sut.DeleteAsync(9999);
        result.Should().BeFalse();
    }

    // --- Helpers ---
    private static TodoItem MakeItem(
        string title,
        string? description = null,
        bool isCompleted = false,
        Priority priority = Priority.Medium) => new()
    {
        Title = title,
        Description = description,
        IsCompleted = isCompleted,
        Priority = priority,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
