# Sabat backend

Backend for the Sabat website. It provides public news and contact endpoints and
administrative endpoints for managing both modules.

## Stack

- ASP.NET Core 10
- Entity Framework Core 10
- PostgreSQL through Npgsql
- Swagger/OpenAPI documentation and interactive API testing
- Docker image compatible with Google Cloud Run

## Configuration

Production values must be supplied as environment variables or Cloud Run
secrets. Do not commit real credentials.

| Environment variable | Purpose |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Admin__ApiKey` | Bootstrap/technical key used in `X-Admin-Key` |
| `Admin__Jwt__SigningKey` | Secret of at least 32 characters used to sign administrator sessions |
| `Cors__AllowedOrigins__0` | First allowed frontend origin |
| `FileStorage__PublicBaseUrl` | Public origin used for uploaded image URLs |
| `Database__ApplyMigrations` | Applies pending migrations during startup when `true` |
| `Database__SeedInitialContent` | Imports the four existing frontend news stories when `true` |

For Cloud SQL, Npgsql can use a Unix socket connection string such as
`Host=/cloudsql/PROJECT:REGION:INSTANCE;Database=sabat;Username=...;Password=...`.
Prefer running migrations as a separate Cloud Run job rather than enabling
automatic migrations on every production instance.

## Local development

1. Start a local PostgreSQL database named `sabat`.
2. Apply migrations with `dotnet ef database update --project Sabat.Infrastructure --startup-project Sabat.API`.
3. Run `dotnet run --project Sabat.API`.
4. Open `/swagger` for the interactive Swagger UI. The generated OpenAPI document is available at `/swagger/v1/swagger.json`.

The example administrative key in `appsettings.Development.json` is for local
development only and must never be reused in production.

## API overview

- `GET /api/news` — published news with language, pagination, search and category filter
- `GET /api/news/latest` — latest news for the home page
- `GET /api/news/{slug}` — one published article
- `POST /api/contact` — submit a contact form
- `POST /api/admin/auth/bootstrap` — create the first administrator using `X-Admin-Key`
- `POST /api/admin/auth/login` — sign in and receive secure session/CSRF cookies
- `GET /api/admin/auth/me` — current administrator profile
- `POST /api/admin/auth/change-password` — change the current password and revoke older sessions
- `POST /api/admin/auth/logout` — revoke sessions and clear cookies
- `/api/admin/news` — protected news management
- `/api/admin/contacts` — protected contact-message management
- `POST /api/admin/news/images` — protected local image upload
- `GET /health` — Cloud Run health endpoint

The first administrator is created once through the bootstrap endpoint with the
technical `X-Admin-Key`. Normal administrator access uses a signed JWT stored in
an `HttpOnly`, `Secure` cookie. State-changing requests from the admin frontend
must also include the `X-CSRF-Token` value returned by login. Browser requests
must use `credentials: "include"`, and the exact frontend origin must be listed
in CORS configuration.

Local image storage is intended for development. Before production deployment,
replace it with Google Cloud Storage because the Cloud Run filesystem is ephemeral.

In Swagger UI, click `Authorize` and enter only the administrative key value.
Swagger adds the `X-Admin-Key` header automatically to API-key protected operations.
