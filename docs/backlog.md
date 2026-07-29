# Delivery Backlog

Backlog order favors early risk reduction and demonstrable vertical value. An
item is “done” only with acceptance criteria, tests, documentation, structured
logging review, and updated daily checklist.

## Phase 0 — Discovery (current)

- [x] Define the problem, personas, vocabulary, assumed scope, and exclusions.
- [x] Propose architecture and record initial ADRs.
- [x] Identify missing requirements that block implementation.
- [ ] Obtain and record the original challenge contract.
- [ ] Confirm fields, relationships, authorization, deletion, and public access.

## Phase 1 — Walking skeleton

- [ ] Scaffold `/apps/api` and `/apps/web` with pinned supported dependencies.
- [ ] Add formatting, analyzers, type-checking, and test commands.
- [ ] Add PostgreSQL and both applications to Docker Compose.
- [ ] Add API Problem Details, Serilog request logging, CORS, OpenAPI, and live/
  ready health endpoints.
- [ ] Render a web shell that calls API health through the configured Axios
  client and TanStack Query.

**Acceptance:** one documented command starts the system; health is visible end
to end; quality commands run locally; no catalog feature exists yet.

## Phase 2 — Identity slice

- [ ] Define authentication threat model and token lifetime/storage decision.
- [ ] Add user schema and safe bootstrap workflow.
- [ ] Implement login validation and JWT issuance.
- [ ] Protect write endpoints and implement frontend session handling.
- [ ] Test valid, invalid, expired, and unauthorized scenarios without logging
  sensitive values.

**Acceptance:** a seeded manager can sign in and protected API behavior is
consistent; credentials and tokens never enter source control or logs.

## Phase 3 — Genres walking feature

- [ ] Update product definition, ADR impact, and day checklist.
- [ ] Implement genre schema, migration, vertical slices, and integration tests.
- [ ] Implement list and form screens with all UI states.

**Acceptance:** the smallest reference-data feature works through UI, API, and
PostgreSQL, establishing conventions for subsequent slices.

## Phase 4 — Authors

- [ ] Confirm author fields and name rules.
- [ ] Implement author slices and constraints with API tests.
- [ ] Implement accessible author screens and mutation feedback.

**Acceptance:** managers can maintain authors and conflicts use the documented
HTTP contract.

## Phase 5 — Books and relationships

- [ ] Confirm book fields and cardinalities.
- [ ] Implement transactional book slices and relational constraints.
- [ ] Add paginated search/filter projection without N+1 queries.
- [ ] Implement book screens with author and genre selection.
- [ ] Cover validation, missing links, duplicates, concurrency, and conflicts.

**Acceptance:** the core catalog journey meets every confirmed challenge
scenario and remains consistent under invalid writes.

## Phase 6 — Delivery hardening

- [ ] Add CI for API tests/build and web lint/typecheck/test/build.
- [ ] Document environment variables, migrations, backup, deployment, and
  rollback.
- [ ] Add Render and Vercel configuration and production CORS origins.
- [ ] Run accessibility, security, performance, and log-sanitization reviews.
- [ ] Prepare seeded demo and English interview architecture narrative.

**Acceptance:** reproducible deployment, verified smoke test, documented
rollback, and no unresolved critical findings.
