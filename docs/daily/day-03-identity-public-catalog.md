# Day 3 — Identity, public catalog, and delivery hardening

**Date:** 2026-07-29

## Objective

Secure catalog writes, expose the anonymous public catalog projection, add
append-only audit rows for deletes, and prepare repeatable delivery checks.

## Acceptance criteria

- A bootstrap catalog manager can be seeded from environment variables.
- Login returns a JWT and indicates whether the password must be changed.
- Users with an expired bootstrap credential cannot create, update, or delete
  catalog records until they change the password.
- Anonymous callers can read public books only when the book is active,
  `publishOnSite = true`, and its Author and Genre are active.
- Book, Author, and Genre deletes remain soft deletes and write an audit row in
  the same save operation.
- The SPA handles sign in, mandatory password change, sign out, and detailed
  validation errors.
- CI, Render, Vercel, and local environment variable guidance are documented.
- Main quality commands pass locally.

## Checklist

- [x] User database model and migration added.
- [x] Bootstrap user workflow added.
- [x] JWT login and token issuance added.
- [x] Mandatory first-login password change enforced.
- [x] Write endpoints protected by authorization policy.
- [x] SPA session handling added.
- [x] Anonymous public catalog endpoints added.
- [x] Soft-delete audit rows added.
- [x] CI workflow added.
- [x] Render and Vercel configuration added.
- [x] Identity and audit tests added.
- [x] CSV-to-PostgreSQL import script.
- [x] Log-sanitization review completed.
- [x] Full accessibility, security, and performance review.

## Development notes

The importer now uses the official CSV export in `docs/input/` instead of the
previous JSON analysis artifact. The database and API include the fallback
Author `Not Identified` and Genre `Unclassified` that the importer relies on.
