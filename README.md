# HelpDesk Backend

Standalone **ASP.NET Core 9** solution following Clean Architecture + CQRS.

## Projects

```
src/
├── HelpDesk.API             # Web API host, controllers, SignalR hub, Program.cs
├── HelpDesk.Application     # MediatR handlers, DTOs, validators, mapping profiles
├── HelpDesk.Domain          # Entities + enums (no external dependencies)
└── HelpDesk.Infrastructure  # EF Core DbContext, repositories, JWT, file storage
```

## Prerequisites

- .NET 9 SDK
- SQL Server 2019+ (or LocalDB / SQL Express)
- EF Core CLI: `dotnet tool install --global dotnet-ef`

## Configuration

Edit `src/HelpDesk.API/appsettings.json` (or use `dotnet user-secrets`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=HelpDesk;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Issuer": "HelpDesk",
    "Audience": "HelpDesk.Clients",
    "SigningKey": "REPLACE_WITH_LONG_RANDOM_STRING_AT_LEAST_64_CHARS",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 14
  },
  "Cors": {
    "AllowedOrigins": [ "https://localhost:5173" ]
  }
}
```

## Run

```bash
dotnet restore
dotnet ef database update --project src/HelpDesk.Infrastructure --startup-project src/HelpDesk.API
dotnet run --project src/HelpDesk.API
```

- API: <https://localhost:5001>
- Swagger: <https://localhost:5001/swagger>
- SignalR hub: `/hubs/notifications`

If you prefer raw SQL provisioning instead of EF migrations, use the scripts
in `../database/` (see `database/README.md`).

## Default seed accounts

| Email                 | Password    | Role  |
| --------------------- | ----------- | ----- |
| `admin@helpdesk.io`   | `Admin#123` | Admin |
| `agent@helpdesk.io`   | `Agent#123` | Agent |
| `user@helpdesk.io`    | `User#123`  | User  |

## Architecture

- **Domain** – entities, value objects, enums
- **Application** – CQRS commands/queries (MediatR), validators (FluentValidation), DTOs, AutoMapper profiles
- **Infrastructure** – EF Core `AppDbContext`, repositories, Unit of Work, JWT service, file storage
- **API** – controllers, SignalR `NotificationsHub`, Serilog, Swagger
