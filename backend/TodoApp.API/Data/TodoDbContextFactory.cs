using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TodoApp.API.Data;

/// <summary>
/// Design-time factory used exclusively by 'dotnet ef migrations' tooling.
/// Forces the Postgres provider so generated migrations and the model snapshot
/// contain Postgres-native column types (boolean, integer, timestamp with time zone)
/// rather than SQLite types.
///
/// This class is NOT used at runtime — EF ignores it when the app is running.
/// </summary>
public class TodoDbContextFactory : IDesignTimeDbContextFactory<TodoDbContext>
{
    public TodoDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<TodoDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time_placeholder;Username=x;Password=x")
            .Options;
        return new TodoDbContext(opts);
    }
}
