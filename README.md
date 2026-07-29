# Books Library

Books Library is the planned catalog-management case study from **AyaX IT
Solutions**. It will manage books, authors, and genres while keeping the domain
language broad enough for a future community and cultural collection at
UBEMTEM.

> **Current status: discovery and architecture baseline.** The repository did
> not include the original challenge statement. In accordance with the project
> workflow, implementation is intentionally gated until the open product
> questions in the [Product Definition](docs/product-definition.md) are answered.

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
8. [Sample data analysis](docs/catalog-data-analysis.md) — provisional field
   profile and remaining full-export checks.
9. [Delivery backlog](docs/backlog.md) — ordered, testable increments.
10. [Day 0 checklist](docs/daily/day-00-discovery.md) — current progress and exit
   criteria.

## Planned stack

| Area | Choice | Purpose |
| --- | --- | --- |
| Web | React, Vite, TypeScript, React Router, TanStack Query, Tailwind CSS, Axios | Accessible, type-safe administration UI |
| API | .NET 8, ASP.NET Core, EF Core, FluentValidation, JWT, Swagger | Explicit HTTP application boundary |
| Data | PostgreSQL | Durable relational catalog model |
| Quality | xUnit, FluentAssertions | Behaviour-focused automated tests |
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
