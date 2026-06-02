# EF Core SQL Server Setup for Render

This project uses EF Core with SQL Server in the API startup (`UseSqlServer`).

## Connection string source

The API reads the connection string from:
- `ConnectionStrings:DefaultConnection`

In Render environment variables, use:
- `ConnectionStrings__DefaultConnection`

Do not hardcode credentials in source files.

## Azure SQL connection string format

Use this format (replace placeholders):

```text
Server=tcp:edusphere-server-123.database.windows.net,1433;
Database=Tabsan-EduSphere;
User Id=superadmin;
Password=<your-password>;
Encrypt=True;
TrustServerCertificate=False;
MultipleActiveResultSets=True;
```

## Render compatibility notes

- Render env var must be named exactly: `ConnectionStrings__DefaultConnection`
- API startup also binds to Render PORT via:
  - `var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";`
  - `app.Urls.Add($"http://0.0.0.0:{port}");`

## Basic migrations workflow

Run from repository root:

```bash
dotnet ef migrations add InitialCreate --project src/Tabsan.EduSphere.Infrastructure/Tabsan.EduSphere.Infrastructure.csproj --startup-project src/Tabsan.EduSphere.API/Tabsan.EduSphere.API.csproj
dotnet ef database update --project src/Tabsan.EduSphere.Infrastructure/Tabsan.EduSphere.Infrastructure.csproj --startup-project src/Tabsan.EduSphere.API/Tabsan.EduSphere.API.csproj
```

If a migration history already exists in your project/database, use a new migration name instead of `InitialCreate`.
