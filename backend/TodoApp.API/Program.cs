using Microsoft.EntityFrameworkCore;
using TodoApp.API.Data;
using TodoApp.API.Middleware;
using TodoApp.API.Repositories;
using TodoApp.API.Services;

// Npgsql by default rejects DateTime values with Kind=Unspecified when writing
// to a 'timestamp with time zone' column. This switch tells Npgsql to treat
// Unspecified as UTC, which matches the intent of DateTime.UtcNow throughout
// the codebase. Must be set before any Npgsql type is first used.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "TodoApp API", Version = "v1" });
});

// --- Database ---
// Switch between SQLite (dev) and PostgreSQL (production/docker) via config.
// The ASPNETCORE_ENVIRONMENT or a DB_PROVIDER env var controls the choice.
// Trade-off: a single connection string key would be simpler, but explicit
// provider switching avoids accidental SQLite usage in production.

var dbProvider = builder.Configuration.GetValue<string>("DbProvider") ?? "sqlite";

if (dbProvider.Equals("postgres", StringComparison.OrdinalIgnoreCase))
{
    var connStr = builder.Configuration.GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Missing 'Postgres' connection string for PostgreSQL provider.");

    builder.Services.AddDbContext<TodoDbContext>(opts =>
        opts.UseNpgsql(connStr));
}
else
{
    var connStr = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=todo.db";
    builder.Services.AddDbContext<TodoDbContext>(opts =>
        opts.UseSqlite(connStr));
}

// --- Application services ---
builder.Services.AddScoped<ITodoRepository, TodoRepository>();
builder.Services.AddScoped<ITodoService, TodoService>();

// --- CORS ---
// Allow the React dev server and the docker-composed frontend.
// In production you'd lock this down to the real domain.
builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(policy =>
        policy.WithOrigins(
                "http://localhost:3000",   // React dev server
                "http://localhost:5173",   // Vite dev server
                "http://frontend:80"       // Docker service
              )
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

// --- Middleware pipeline ---
// ErrorHandlingMiddleware must be first so it catches exceptions from all
// subsequent middleware and controllers.
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();

// --- Auto-migrate on startup ---
// Runs before app.Run() so the schema is guaranteed to exist before
// the first request is served. If migration fails the app crashes fast
// with a clear error rather than serving requests against a missing schema.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Applying database migrations...");
    await db.Database.MigrateAsync();
    logger.LogInformation("Database migrations applied successfully.");
}

app.Run();

// Needed for integration test WebApplicationFactory
public partial class Program { }
