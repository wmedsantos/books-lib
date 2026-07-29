# ADR 0002: REST API with explicit server-state management

- **Status:** Proposed
- **Date:** 2026-07-29

## Context

The React application and API deploy separately. Catalog screens need caching,
loading/error handling, mutation feedback, and predictable URLs. The challenge
requires React Router, TanStack Query, and Axios.

## Decision

Expose versioned resource-oriented JSON endpoints from ASP.NET Core. Use RFC
7807 Problem Details for errors. In the web app, use React Router for navigation,
one configured Axios client for transport concerns, and TanStack Query for
remote data lifecycle. Keep transient form and presentation state local to the
feature.

Do not duplicate server records in a global client store. Query keys will be
feature-owned and mutations will invalidate only affected collections/details.

## Consequences

### Positive

- HTTP semantics and error contracts remain usable by a future public site or
  another client.
- Server cache concerns are not mixed with view state.
- Feature ownership keeps changes local and reduces accidental invalidation.

### Negative

- DTO types exist on both sides unless contract generation is later justified.
- Cache invalidation requires a disciplined query-key convention.
- REST may require several requests for composite future screens.

## Alternatives considered

- **GraphQL:** rejected because the known screens do not need client-selected
  graphs, and its schema/runtime/authorization complexity has no current payoff.
- **Redux for all state:** rejected because it duplicates TanStack Query's
  responsibility and adds synchronization work.
- **Generated client immediately:** deferred; generation improves contract
  consistency but adds build tooling before the endpoint surface is stable.

## Revisit when

Multiple clients expose contract drift, composite reads create measurable
round-trip problems, or offline editing becomes a validated requirement.
