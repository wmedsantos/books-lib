# Architecture

**Status:** Proposed<br>
**Decision horizon:** MVP plus one likely UBEMTEM adaptation

## Context

The team is currently one developer, the domain is small, and operational cost
matters. The architecture therefore optimizes for a short feedback loop and
clear feature ownership, not hypothetical scale.

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
`Authors`, and `Genres`. Within a module, folders are organized by use case—for
example `Books/Create`, `Books/List`, and `Books/Update`—rather than by a global
technical layer.

Each slice owns its request/response contract, FluentValidation validator,
application handler, endpoint mapping, and tests. Domain entities and EF Core
configuration remain internal to the module where practical. Cross-cutting
infrastructure is limited to authentication, persistence setup, error mapping,
logging, and health checks.

This applies Clean Architecture at boundaries, not as four projects full of
pass-through interfaces. The HTTP layer does not contain business rules, and
domain/application code does not depend on controllers. We will introduce an
interface only when there is a real alternate implementation or a useful test
seam.

### Web: route-oriented features

React Router defines screen boundaries. Each feature owns its components,
query/mutation hooks, form schema/types, and API calls. Axios provides one
configured transport client; TanStack Query owns server-state caching and
invalidation. Local UI state stays in React. A global state library is not
justified for the MVP.

The API remains the source of truth. TypeScript response types describe the
wire contract but are not treated as domain entities.

## Data design

The proposed relational model uses `books`, `authors`, `genres`, `book_authors`,
`book_genres`, and `users`. Join tables have composite unique keys and foreign
keys. Normalized columns used for uniqueness are an implementation option to be
confirmed when exact case/diacritic rules are known.

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
custom cryptography. JWT bearer authentication is appropriate for a separately
hosted SPA/API, but token storage and refresh strategy remain an explicit threat
model decision before implementation. Secrets are environment variables.

Serilog emits structured JSON in production. Request logs carry trace IDs and
avoid request bodies. `/health/live` checks the process; `/health/ready` checks
database connectivity so Render can distinguish restart from traffic readiness.

Docker Compose provides API, web, and PostgreSQL for local development. Render
hosts the containerized API and database; Vercel hosts static frontend assets.
This is cheaper and easier to operate than Kubernetes or multiple services.

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
