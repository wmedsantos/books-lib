# Delivery Backlog

Backlog order favors early risk reduction and demonstrable vertical value. An
item is “done” only with acceptance criteria, tests, documentation, structured
logging review, and updated daily checklist.

## Phase 0 — Discovery (current)

- [x] Define the problem, personas, vocabulary, assumed scope, and exclusions.
- [x] Propose architecture and record initial ADRs.
- [x] Identify missing requirements that block implementation.
- [x] Extract the supplied challenge statement into traceable requirements.
- [x] Analyze the three-record UBEMTEM sample and document provisional mapping.
- [x] Analyze the full UBEMTEM export or an aggregate profile.
- [x] Confirm relationships, authorization, deletion, and public access.
- [x] Confirm delivery requirements from the challenge statement.
- [x] Confirm provisional Book fields from the sample.
- [x] Confirm `Unclassified` as the missing-Genre import fallback.
- [x] Confirm dataset-wide field quality before executing an import.

## Day 1 — Walking skeleton and reference-data path

- [x] Scaffold `/apps/api` and `/apps/web` with pinned supported dependencies.
- [x] Add formatting, analyzers, type-checking, and test commands.
- [x] Add PostgreSQL and both applications to Docker Compose.
- [x] Add API Problem Details, Serilog request logging, CORS, OpenAPI, and live/
  ready health endpoints.
- [x] Render a web shell that calls API health through the configured Axios
  client and TanStack Query.
- [x] Complete the Genre API and UI walking feature with its main tests.

**Acceptance:** one documented command starts the system; health is visible end
to end; quality commands run locally; Genre management works through the SPA,
API, and PostgreSQL with main-scenario tests.

## Day 2 — Authors, books, and core relationships

- [x] Complete Author management through API and SPA.
- [x] Complete Book management with required Author and Genre relationships.
- [x] Add search, pagination, and relevant filters.
- [x] Add API tests for main success and failure paths.

**Acceptance:** every mandatory domain operation works end to end and book lists
display their author and genre.

## Day 3 — Identity, public catalog, and delivery hardening

- [x] Define authentication threat model and token lifetime/storage decision.
- [x] Add user schema and safe bootstrap workflow.
- [x] Implement login validation and JWT issuance.
- [x] Implement mandatory first-login password change; deny catalog operations
  while the credential is expired.
- [x] Protect write endpoints and implement frontend session handling.
- [x] Smoke-test valid login, mandatory password change, unauthorized writes,
  forbidden expired-credential writes, and authorized writes.
- [ ] Add automated authentication integration tests for invalid credentials,
  JWT expiration, authorization failures, and log sanitization.

- [x] Implement an anonymous public projection restricted to active books with
  `publishOnSite = true` and active related records.
- [x] Soft-delete in the same transaction as an append-only audit entry.
- [ ] Implement a rerunnable JSON-to-PostgreSQL import script with dry-run mode,
  per-row validation, `Unclassified` Genre fallback, `Not Identified` Author
  fallback, duplicate strategy, transactional batches, and an outcome/error
  report.
- [ ] Document import prerequisites, command, rollback, and source-file handling;
  never embed the catalog export in the application image.
- [ ] Add automated tests for normalization, author resolution, fallback Author,
  fallback Genre, duplicates, invalid rows, and safe `publishOnSite = false`
  defaults.
- [x] Add CI for API tests/build and web lint/typecheck/test/build.
- [x] Document environment variables, migrations, backup, deployment, and
  rollback.
- [x] Add Render and Vercel configuration and production CORS origins.
- [ ] Run accessibility, security, performance, and log-sanitization reviews.
- [ ] Prepare seeded demo and English interview architecture narrative.

**Acceptance:** reproducible deployment, verified smoke test, documented
rollback, no unresolved critical findings, and every row in the requirement
traceability matrix has evidence or an explicit known limitation.
