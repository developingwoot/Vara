# Contributing to VARA

## Getting Started

1. Fork the repo and clone your fork
2. Follow the development setup in [README.md](README.md)
3. Create a feature branch: `git checkout -b feature/my-feature`

## Running Tests

```bash
# Backend tests (no database required — uses SQLite in-memory)
dotnet test tests/Vara.Tests/

# Frontend type checking
cd src/vara-frontend
npm run check
```

All 122 backend tests must pass before submitting a PR.

## Adding Migrations

After modifying an EF Core entity:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Vara.Api/Vara.Api.csproj
```

Migrations run automatically on startup via `db.Database.MigrateAsync()`.

## Code Style

**Backend (C#)**
- Minimal API pattern — endpoints in `src/Vara.Api/Endpoints/`
- Services behind interfaces in `src/Vara.Api/Services/`
- Use `AsNoTracking()` on all read-only EF queries
- Validate request DTOs with FluentValidation filters, not inline

**Frontend (Svelte/TypeScript)**
- Svelte 5 runes only (`$state`, `$derived`, `$effect`) — no Svelte 4 reactivity
- Design tokens from `src/app.css` — no hardcoded colours or sizes
- All numbers and metrics use `font-family: var(--font-mono)`
- Replace `any` with proper TypeScript interfaces matching backend DTOs

## PR Process

1. One logical change per PR
2. Link related issues
3. Ensure CI passes (backend build + test, frontend check + build)
4. Squash merge preferred

## Reporting Issues

Use [GitHub Issues](https://github.com/YOUR_USERNAME/vara/issues). Include:
- Steps to reproduce
- Expected vs actual behaviour
- Browser / .NET version if relevant
