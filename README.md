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
python3 -m unittest tests/import_catalog_csv_tests.py
npm --prefix apps/web run lint
npm --prefix apps/web run build
```

The web runtime dependency audit is clean with `npm --prefix apps/web audit
--omit=dev`. The full audit still reports the known Vite 5 development-server
advisory; fixing it requires a Vite major upgrade that drops compatibility with
the Node version currently installed on this machine.

## Logs

The API uses Serilog request logging and currently writes logs only to standard
output through the console sink. In Docker, read them with:

```bash
docker compose logs api
```

No file, database table, or external log sink is configured in this repository.
Request logs are intentionally limited to method, path without query string,
status code, and elapsed time. The application does not log request bodies,
`Authorization` headers, passwords, or JWT values.

## Production publish target

The admin SPA is configured to be published under its own subdomain:

```text
https://biblio.ubemtem.org
```

For Vercel, [apps/web/vercel.json](apps/web/vercel.json) builds the Vite app
at the domain root and rewrites all routes to the SPA entrypoint.

Required frontend production environment variable:

```bash
VITE_API_BASE_URL=https://replace-with-api-host
```

Required API CORS production setting:

```bash
Cors__AllowedOrigins__0=https://biblio.ubemtem.org
```

If the public UBEMTEM site continues to be served from the separate
`wmedsantos/ubemtem` repository, using this subdomain avoids coupling that site
to the admin SPA deployment.

## Production API on Render

The backend is configured for Render with [render.yaml](render.yaml). The
Blueprint creates:

- a Docker web service named `bookslib-api`
- a Render Postgres database named `bookslib-db`
- `DATABASE_URL` wired from the database internal connection string
- CORS restricted to `https://biblio.ubemtem.org`
- a generated `Jwt__SigningKey`

Create the Blueprint from the Render dashboard using this GitHub repository.
During the initial Blueprint creation, provide:

```bash
Bootstrap__Email=your-admin-email
Bootstrap__Password=temporary-first-login-password
```

After the first successful deploy, verify:

```text
https://bookslib-api.onrender.com/health/ready
```

Then set the frontend production variable in Vercel to the Render API origin:

```bash
VITE_API_BASE_URL=https://bookslib-api.onrender.com
```

Redeploy the Vercel frontend after changing that variable. If you later add a
custom API domain, update both `VITE_API_BASE_URL` in Vercel and the API CORS
origin only if the frontend origin changes.

## Import the source catalog

The source catalog export is read from `docs/input/library_20260729_190704.csv`.
The importer writes directly to PostgreSQL and is designed to be run after the
API has applied migrations.

Create a local Python environment and install the importer dependency:

```bash
python3 -m venv .venv
.venv/bin/python -m pip install -r scripts/requirements.txt
```

Validate the CSV and database writes without keeping any changes:

```bash
.venv/bin/python scripts/import_catalog_csv.py --dry-run --report /tmp/booklib-import-report.json
```

The `--dry-run` command intentionally rolls back the transaction, so it will not
change what appears in the web app.

Run the import against the local Docker Compose database:

```bash
.venv/bin/python scripts/import_catalog_csv.py --report /tmp/booklib-import-report.json
```

To fill missing cover URLs, add `--fetch-covers`. The importer first checks the
local enriched Libib JSON at
`docs/input/biblioteca_ubemtem_com_capas_libib_final.json` by ISBN and then by
title:

```bash
.venv/bin/python scripts/import_catalog_csv.py --fetch-covers --report /tmp/booklib-import-report.json
```

To also try Google Books by ISBN when the local JSON has no cover, provide a
Google Books API key through an environment variable:

```bash
GOOGLE_BOOKS_API_KEY=your-api-key \
.venv/bin/python scripts/import_catalog_csv.py --fetch-covers --report /tmp/booklib-import-report.json
```

The cover lookup also works after the catalog has already been imported: rows
detected as duplicates are skipped, but missing `cover_url` values on existing
books are updated when a cover is found.

The script defaults to:

- database URL: `postgresql://bookslib:bookslib_dev@localhost:5432/bookslib`
- source file: `docs/input/library_20260729_190704.csv`
- `publishOnSite = false` for every imported book
- Genre fallback: `Unclassified`
- Author fallback: `Not Identified`

Use `BOOKSLIB_DATABASE_URL` or `--database-url` to target another PostgreSQL
database. Use `--publish-on-site` only for a reviewed source where public
visibility is intentional.

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
13. [Delivery review](docs/delivery-review.md) — accessibility, security,
    performance, and log review.
14. [Interview narrative](docs/interview-narrative.md) — seeded demo flow and
    English architecture explanation.

## Stack

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

## Repository shape

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
