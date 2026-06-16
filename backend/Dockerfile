# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first to take advantage of layer caching.
# Only the package restore layer is invalidated when code changes, not when
# project files change.
COPY TodoApp.sln ./
COPY TodoApp.API/TodoApp.API.csproj TodoApp.API/
COPY TodoApp.Tests/TodoApp.Tests.csproj TodoApp.Tests/

RUN dotnet restore

# Copy the rest of the source
COPY . .

# Publish the API project (release mode, self-contained = false -> smaller image)
RUN dotnet publish TodoApp.API/TodoApp.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .

# Expose port 8080 (ASP.NET Core default in containers)
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "TodoApp.API.dll"]
