# Strategico Admin Action Audit (Demo)

Lightweight audit logging for admin actions, designed for operational traceability.

## Features
- Dummy login: act as User or Admin
- User action updates inventory and logs an audit event
- Admin dashboard shows latest events + runs two core investigation queries:
  - What actions did user X perform last week?
  - What changed on record Y?

## Tech
- ASP.NET Core
- Razor Pages (simple demo UI)
- EF Core + PostgreSQL

## Run locally
1. Start Postgres (Docker):
   docker run --name strategico-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_USER=postgres -e POSTGRES_DB=strategico_audit -p 5432:5432 -d postgres:16

2. Apply migrations:
   dotnet ef database update

3. Run:
   dotnet run

Open:
- /login
- /user/inventory-adjust
- /admin/audit
