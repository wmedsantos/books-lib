# Architecture

**Status:** Current production architecture<br>
**Decision horizon:** MVP plus one likely UBEMTEM adaptation

## Context

The team is currently one developer, the domain is small, and operational cost
matters. The architecture therefore optimizes for a short feedback loop and
clear feature ownership, not hypothetical scale. The challenge has a three-day
timebox and explicitly values a functional, coherent solution over exhaustive
scope, so every structural choice must pay for itself during implementation or
make the result materially easier to evaluate.

## System context

```text
[Catalog manager / visitor]
              |
              v
[React SPA on Vercel] -- HTTPS/JSON --> [ASP.NET Core API on Render]
                                               |
                                               v
                                    [Managed PostgreSQL]
```

The browser never connects directly to PostgreSQL. The API is the authorization,
validation, transaction, and compatibility boundary.

## Application architecture

### API: modular monolith with vertical slices

One deployable ASP.NET Core process contains modules for `Identity`, `Books`,
`Authors`, and `Genres`. Within a module, files are organized around the
feature boundary: endpoint mapping, request/response contracts, field
validation, entity behavior, EF Core mapping, and tests.

Each slice owns its HTTP contract, validation rules, endpoint behavior, and
tests. Cross-cutting infrastructure is limited to authentication, persistence
setup, error mapping, logging, and health checks.

This applies Clean Architecture at boundaries, not as four projects full of
pass-through interfaces. The HTTP layer does not contain business rules, and
domain/application code does not depend on controllers. We will introduce an
interface only when there is a real alternate implementation or a useful test
seam.

### Web: route-oriented features

The current React app is a compact single-page admin shell with tabbed sections
for books, authors, and genres. Feature state, form state, language preference,
session state, and pagination stay in React. Axios provides one configured
transport client; TanStack Query owns server-state caching and invalidation. A
global state library is not justified for the MVP.

The API remains the source of truth. TypeScript response types describe the
wire contract but are not treated as domain entities.

## Data design

The relational model uses `books`, `authors`, `genres`, `users`, and
`audit_entries`. A book has required `author_id` and `genre_id` foreign keys;
join tables would misrepresent the confirmed many-to-one rules. Catalog tables
carry deletion metadata and books carry `publish_on_site`. Normalized columns
used for uniqueness remain an implementation option until exact case/diacritic
rules are known.

EF Core migrations are versioned with the API. Write use cases save once per
transaction. Read use cases project directly into response DTOs and use
`AsNoTracking` to avoid loading unnecessary graphs.

## HTTP conventions

- Resource URLs use plural nouns under `/api/v1`.
- `GET` is safe, `PUT` replaces editable resource state, `POST` creates, and
  `DELETE` removes or archives according to the confirmed product rule.
- Creation returns `201 Created` with a `Location` header; deletion returns
  `204 No Content`.
- Validation uses `400`, missing resources `404`, relationship/deletion
  conflicts `409`, and authentication failures `401`/`403`.
- Errors use `application/problem+json` and include a trace identifier.
- Pagination metadata is part of a stable response envelope; single-resource
  responses are not needlessly wrapped.

## Security and operations

Passwords are hashed with ASP.NET Core's maintained password hasher rather than
custom cryptography. A parameterized operational SQL script bootstraps the first
manager from an externally generated compatible hash and marks the credential
expired. The supplied plaintext temporary password must be delivered through a
secret channel, never committed or logged. JWT bearer authentication is
appropriate for a separately hosted SPA/API, but token storage and refresh
strategy remain an explicit threat-model decision before implementation.
Secrets are environment variables.

Anonymous access is isolated in a public-catalog slice whose query always
applies active-record and `publish_on_site` predicates. It returns a dedicated
public DTO rather than the administrative representation. This prevents a
future admin-only field from becoming public by accident.

Soft deletion is applied explicitly in delete use cases and enforced in query
projections. The same database transaction inserts an append-only audit entry,
so a successful deletion cannot exist without its operation log. This small,
purpose-specific audit trail is preferred over a generic event-sourcing or
full-history subsystem.

Serilog emits request logs to standard output. Request logs avoid request
bodies, query strings, authorization headers, passwords, and JWT values.
`/health/live` checks the process; `/health/ready` checks database connectivity
so Render can distinguish restart from traffic readiness.

Docker Compose provides API, web, and PostgreSQL for local development. In
production, Vercel hosts the static frontend at `https://biblio.ubemtem.org`;
Render hosts the containerized API at
`https://books-lib-yy5q.onrender.com`; and Render PostgreSQL stores catalog
data. The UBEMTEM public site remains separate, so the admin app uses its own
subdomain rather than `/biblio-admin` under the main site.

## Quality strategy

The test pyramid emphasizes fast domain/validator tests and API integration
tests against PostgreSQL-compatible infrastructure. Endpoint tests verify the
real serialization, validation, auth, EF mapping, and Problem Details boundary.
Frontend tests focus on important user behavior rather than implementation
details. A small deployment smoke test verifies health and the main read path.

## Trade-offs and rejected options

| Option | Decision | Reason |
| --- | --- | --- |
| Microservices | Reject | Independent deployment and messaging add failure modes without independent teams or scaling needs. |
| Generic repository/unit of work | Reject initially | EF Core already provides these semantics; wrappers often hide useful query capabilities. |
| Full four-layer Clean Architecture solution | Reject initially | More projects and mappings do not improve this small domain; boundaries can be enforced within modules. |
| CQRS framework/mediator | Defer | Separating read/write use cases is useful; adding a mediator dependency is not required to do it. |
| Generic `ContentItem` hierarchy | Defer | Future cultural-content invariants are unknown; premature generalization would be harder to reverse. |
| Redux/global client store | Reject initially | TanStack Query plus local state covers server and screen state with less synchronization. |
| Refresh tokens | Decide after threat model | They improve session continuity but add rotation, revocation, storage, and abuse cases. |

## Evolution path

New UBEMTEM capabilities should enter as modules or new vertical slices. Extract
a service only when a module needs independent deployment, scaling, ownership,
or availability—not merely because it has a name. Audit history and additional
content types should be modeled after stakeholder discovery, with migrations
and backwards-compatible API contracts.
