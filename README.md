# PDF Data Comparison Admin (ASP.NET Core 8)

Production-ready enterprise admin panel scaffold for secure PDF data comparison workflows.

## Highlights
- ASP.NET Core 8 MVC with Identity authentication.
- SQL Server + EF Core schema for users, roles, permissions, jobs, submissions, and audit logs.
- Policy-based authorization with page/action permission keys.
- Premium SaaS-style UI (dark sidebar + bright card canvas).
- PDF comparison screen with layout switching, mismatch tracking, and guarded submit.
- Excel export implementation using ClosedXML.

## Default Credentials
- Username: `superadmin`
- Password: `SuperAdmin!23`

## Run
1. Update `ConnectionStrings:DefaultConnection` in `appsettings.json`.
2. Run:
   - `dotnet restore`
   - `dotnet ef database update`
   - `dotnet run --project PdfDataComparison.Admin`

## Core Structure
- `Domain/Entities`: core models.
- `Data`: DbContext.
- `Application/*`: interfaces, view models, authorization requirement.
- `Infrastructure/*`: authorization handler/transformation, seed, services.
- `Controllers`: module controllers.
- `Views`: premium admin pages and reusable layout partials.
- `wwwroot/css|js`: UI design system + interaction layer.
