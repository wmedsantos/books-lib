# Books Library

Books Library is the catalog-management case study from **AyaX IT
Solutions**. It will manage books, authors, and genres while keeping the domain
language broad enough for a future community and cultural collection at
UBEMTEM.

> **Current status:** Day 3 identity and delivery hardening. The repository now
> contains a .NET 8 API, React/Vite SPA, PostgreSQL Compose setup, JWT-secured
> write flows, public catalog reads, health checks, and management flows for
> Books, Authors, and Genres.

## Run Locally

Prerequisites:

- .NET SDK 8.0
- Node.js 18.17 or newer
- Docker 24 or newer

Start the full local system:

```bash
docker compose up --build
```

Local URLs:

- Web: http://localhost:5173
- API health: http://localhost:5080/health/ready
- Swagger: http://localhost:5080/swagger

The API applies EF Core migrations at startup and seeds the system Author
`Not Identified` plus the system Genre `Unclassified`. To seed the first
catalog manager locally, provide bootstrap credentials before startup:

```bash
BOOKSLIB_BOOTSTRAP_EMAIL=admin@bookslib.local \
BOOKSLIB_BOOTSTRAP_PASSWORD=ChangeMe123! \
BOOKSLIB_JWT_SIGNING_KEY=local-development-signing-key-change-before-production-12345 \
docker compose up --build
```

The bootstrap password is temporary. The first successful login requires a
password change before write operations are allowed.

Run quality checks:

```bash
dotnet test BooksLib.sln
npm --prefix apps/web run lint
npm --prefix apps/web run build
```

The web runtime dependency audit is clean with `npm --prefix apps/web audit
--omit=dev`. The full audit still reports the known Vite 5 development-server
advisory; fixing it requires a Vite major upgrade that drops compatibility with
the Node version currently installed on this machine.

## Start here

1. [Product Definition](docs/product-definition.md) — users, domain, scope,
   assumptions, acceptance criteria, and open questions.
2. [Requirements traceability](docs/requirements-traceability.md) — challenge
   obligations, planned evidence, and current delivery status.
3. [Architecture](docs/architecture.md) — proposed system shape and trade-offs.
4. [ADR 0001](docs/adr/0001-modular-monolith-and-vertical-slices.md) — backend
   architecture decision.
5. [ADR 0002](docs/adr/0002-rest-api-and-client-state.md) — API and frontend
   boundary decision.
6. [ADR 0003](docs/adr/0003-publication-soft-delete-audit-and-bootstrap.md)
   — public catalog, deletion/audit, cardinality, and identity bootstrap.
7. [ADR 0004](docs/adr/0004-libib-import-and-author-mapping.md) — source-data
   mapping and single-author compatibility.
8. [Catalog data analysis](docs/catalog-data-analysis.md) — full export profile
   and import implications.
9. [Delivery backlog](docs/backlog.md) — ordered, testable increments.
10. [Day 1 checklist](docs/daily/day-01-walking-skeleton.md) — walking skeleton
    progress and exit criteria.
11. [Day 2 checklist](docs/daily/day-02-authors-books.md) — authors, books,
    relationships, and validation.
12. [Day 3 checklist](docs/daily/day-03-identity-public-catalog.md) — identity,
    public catalog, audit, and delivery hardening.

## Planned stack

| Area | Choice | Purpose |
| --- | --- | --- |
| Web | React, Vite, TypeScript, TanStack Query, Axios | Accessible, type-safe administration UI |
| API | .NET 8, ASP.NET Core, EF Core, FluentValidation, JWT, Swagger | Explicit HTTP application boundary |
| Data | PostgreSQL | Durable relational catalog model |
| Quality | xUnit, TypeScript build checks | Behaviour-focused automated tests and fast client feedback |
| Operations | Docker Compose, Render, Vercel, Serilog, Health Checks | Repeatable local operation and low-cost deployment |

Dependencies will be added only with the feature that needs them. This keeps
the initial system explainable and prevents a generated scaffold from becoming
accidental architecture.

## Planned repository shape

```text
apps/
  api/  # ASP.NET Core modular monolith
  web/  # React single-page application
docs/
  adr/
  daily/
```

## Working agreement

Before implementing a feature, update the Product Definition, add or amend the
relevant ADR, and create/update that day's checklist. A slice is complete only
when its acceptance criteria, tests, API documentation, and operational impact
have been addressed.

## License

This project is licensed under GPL-3.0. See [LICENSE](LICENSE).
