# Accessibility, Security, Performance, and Log Review

**Date:** 2026-07-29

This review records the final delivery checks for the Books Library challenge.
It is intentionally scoped to repository evidence and local smoke testing, not
to a third-party penetration test or formal WCAG audit.

## Summary

No critical blocker remains for the challenge delivery. The application has a
working administrative SPA, secured write operations, public read projection,
repeatable import script, automated tests, and documented local operation.

Known limitations are listed below so they can be discussed explicitly during
review instead of being hidden.

## Accessibility

### Reviewed evidence

- Form fields use visible labels and stable `htmlFor` / `id` links on the main
  book, author, genre, login, and password-change forms.
- API validation errors are rendered both as a summary and as field-level
  messages near the invalid control.
- Invalid fields receive `aria-invalid` and a light red background.
- The main navigation uses buttons with active state and an `aria-label`.
- Book cover images include alt text derived from the book title. Placeholder
  covers are marked `aria-hidden`.
- Language selection persists in a cookie and updates `document.documentElement.lang`.
- The layout is keyboard-operable through native inputs, selects, buttons, and
  links.
- Responsive CSS avoids overlapping controls on narrow screens.

### Remaining accessibility risks

- No automated axe/Playwright accessibility scan is configured yet.
- Focus management is basic: after create/update/delete, focus is not moved to
  the changed row or success state.
- Some server-side validation messages remain English because they originate
  from the API. The UI labels are translated, but the API error text is not yet
  localized.

## Security

### Reviewed evidence

- Administrative writes require JWT bearer authentication.
- Catalog writes require the `CatalogWrite` authorization policy and the
  `pwd_expired=false` claim.
- Bootstrap users are seeded only from environment variables and must change the
  temporary password before catalog writes are allowed.
- Passwords are stored as ASP.NET Core Identity password hashes, not plaintext.
- JWT issuer, audience, signing key, and expiration are configurable.
- The API fails startup when `Jwt:SigningKey` is missing.
- CORS is restricted to configured allowed origins.
- Public catalog endpoints are anonymous but return only active books with
  `publishOnSite = true` and active related Author and Genre rows.
- Soft deletes append audit rows in the same save operation.
- The importer defaults `publishOnSite = false`.
- Swagger supports Bearer JWT authorization for manual testing.

### Automated security coverage

- Invalid credentials return `401`.
- Missing token on write returns `401`.
- JWT with expired bootstrap credential returns `403`.
- Password change returns a non-expired token that can write.
- Expired JWT returns `401`.
- Log sanitizer tests verify request logging avoids query strings and sensitive
  template fields.

### Remaining security risks

- JWTs are stored in browser `localStorage`. This is acceptable for the
  challenge scope, but a hardened production system should evaluate httpOnly
  secure cookies and CSRF controls.
- There is no refresh-token flow or token revocation list.
- There is no rate limiting on login.
- Password complexity is minimal: only length is currently enforced for password
  change.
- Audit entries are written, but there is no administrative audit-query UI or
  retention policy yet.
- Deployment secrets must be configured in Render/Vercel dashboards, never
  committed to the repository.

## Performance

### Reviewed evidence

- List endpoints use pagination with bounded `pageSize` up to 100.
- Book list queries project directly to response DTOs and include Author and
  Genre names without an EF Core N+1 loop.
- Search and filters are pushed to PostgreSQL through EF Core queries.
- Common lookup columns have indexes: normalized names, normalized book titles,
  author IDs, genre IDs, ISBN-10, and ISBN-13.
- The SPA uses TanStack Query for request caching and invalidation.
- Imported book publication defaults avoid exposing unreviewed records publicly.
- The CSV importer is rerunnable and reports duplicates instead of repeatedly
  inserting the same catalog rows.

### Remaining performance risks

- Search uses `Contains` on normalized text, which may become slow for a much
  larger catalog. PostgreSQL trigram or full-text indexes would be the next step.
- The Google Books cover lookup is sequential and intentionally conservative.
  It is appropriate for a 105-row import, but bulk imports should add
  concurrency limits and retry/backoff.
- No browser performance budget or Lighthouse report is committed.
- No load test is included.

## Log Review

### Current log persistence

The API writes logs only to standard output through Serilog's console sink. In
local Docker, logs are available with:

```bash
docker compose logs api
```

No application file sink, database table, or external log sink is configured in
this repository. Persistence and retention are therefore owned by the runtime
environment, such as Docker's logging driver locally or the hosting provider's
log stream in production.

### Sanitization policy

Request logs are limited to:

- HTTP method
- request path without query string
- status code
- elapsed time

The application does not log request bodies, `Authorization` headers, passwords,
JWT values, or query strings. Unhandled exception logging includes method and
path only, and Problem Details responses expose a trace ID without sensitive
payloads.

## Commands Run

```bash
dotnet test BooksLib.sln
python3 -m unittest tests/import_catalog_csv_tests.py
npm --prefix apps/web run lint
npm --prefix apps/web run build
git diff --check
```

All commands passed during the final review cycle.
