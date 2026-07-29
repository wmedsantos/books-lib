# Day 1 — Walking skeleton and reference-data path

**Date:** 2026-07-29

## Objective

Create the first executable vertical slice: local infrastructure, API and web
shells, shared quality commands, health checks, and Genre management through
React, ASP.NET Core, and PostgreSQL.

## Backlog

1. Scaffold `/apps/api` and `/apps/web` with pinned supported dependencies.
2. Add formatting, analyzers, type-checking, and test commands.
3. Add PostgreSQL, API, and web services to Docker Compose.
4. Configure Problem Details, structured request logging, CORS, OpenAPI, and
   `/health/live` plus `/health/ready`.
5. Render a web shell that calls API health through Axios and TanStack Query.
6. Implement Genre create, list/search, update, and soft-delete through API and
   SPA.
7. Add focused tests for health, validation, persistence, conflict behavior, and
   the main UI path.
8. Update README with the one-command local startup and quality commands.

## Acceptance criteria

- One documented command starts PostgreSQL, API, and web locally.
- API liveness and database readiness are visible from both Swagger and the web
  shell.
- Genre management works end to end through the SPA, API, and PostgreSQL.
- The system Genre `Unclassified` exists deterministically and cannot be renamed
  or deleted through normal Genre management.
- Main quality commands run locally and are documented.
- No secrets are committed, logged, or embedded in default configuration.

## Checklist

- [x] API project scaffolded.
- [x] Web project scaffolded.
- [x] Docker Compose added.
- [x] Formatting, analyzers, type-checking, and test commands added.
- [x] Health endpoints added and verified by build/runtime checks.
- [x] API error handling and request logging configured.
- [x] Genre database model and migration added.
- [x] Genre API implemented.
- [x] Genre UI implemented.
- [x] Tests added for the main Day 1 domain rules.
- [x] README updated.

## Development notes

The full UBEMTEM export has been profiled and does not block the Day 1 walking
skeleton. Import execution remains a later slice, but the known edge cases are
resolved: missing source authors use `Not Identified`, same-title/same-ISBN
duplicates are discarded, and same-title/different-ISBN rows are imported with a
period appended to the later title.
