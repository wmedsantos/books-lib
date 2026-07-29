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
- [ ] Analyze the full UBEMTEM export or an aggregate profile.
- [x] Confirm relationships, authorization, deletion, and public access.
- [x] Confirm delivery requirements from the challenge statement.
- [x] Confirm provisional Book fields from the sample.
- [x] Confirm `Unclassified` as the missing-Genre import fallback.
- [ ] Confirm dataset-wide field quality before executing an import.

## Day 1 — Walking skeleton and reference-data path

- [ ] Scaffold `/apps/api` and `/apps/web` with pinned supported dependencies.
- [ ] Add formatting, analyzers, type-checking, and test commands.
- [ ] Add PostgreSQL and both applications to Docker Compose.
- [ ] Add API Problem Details, Serilog request logging, CORS, OpenAPI, and live/
  ready health endpoints.
- [ ] Render a web shell that calls API health through the configured Axios
  client and TanStack Query.
- [ ] Complete the Genre API and UI walking feature with its main tests.

**Acceptance:** one documented command starts the system; health is visible end
to end; quality commands run locally; Genre management works through the SPA,
API, and PostgreSQL with main-scenario tests.

## Day 2 — Authors, books, and core relationships

- [ ] Complete Author management through API and SPA.
- [ ] Complete Book management with required Author and Genre relationships.
- [ ] Add search, pagination, and relevant filters.
- [ ] Add API tests for main success and failure paths.

**Acceptance:** every mandatory domain operation works end to end and book lists
display their author and genre.

## Day 3 — Identity, public catalog, and delivery hardening

- [ ] Define authentication threat model and token lifetime/storage decision.
- [ ] Add user schema and safe bootstrap workflow.
- [ ] Implement login validation and JWT issuance.
- [ ] Implement mandatory first-login password change; deny catalog operations
  while the credential is expired.
- [ ] Protect write endpoints and implement frontend session handling.
- [ ] Test valid, invalid, expired, and unauthorized scenarios without logging
  sensitive values.

- [ ] Implement an anonymous public projection restricted to active books with
  `publishOnSite = true` and active related records.
- [ ] Soft-delete in the same transaction as an append-only audit entry.
- [ ] Implement a rerunnable JSON-to-PostgreSQL import script with dry-run mode,
  per-row validation, `Unclassified` Genre fallback, duplicate strategy,
  transactional batches, and an outcome/error report.
- [ ] Document import prerequisites, command, rollback, and source-file handling;
  never embed the catalog export in the application image.
- [ ] Add automated tests for normalization, author resolution, fallback Genre,
  duplicates, invalid rows, and safe `publishOnSite = false` defaults.
- [ ] Add CI for API tests/build and web lint/typecheck/test/build.
- [ ] Document environment variables, migrations, backup, deployment, and
  rollback.
- [ ] Add Render and Vercel configuration and production CORS origins.
- [ ] Run accessibility, security, performance, and log-sanitization reviews.
- [ ] Prepare seeded demo and English interview architecture narrative.

**Acceptance:** reproducible deployment, verified smoke test, documented
rollback, no unresolved critical findings, and every row in the requirement
traceability matrix has evidence or an explicit known limitation.
