# Seeded Demo and English Interview Narrative

**Date:** 2026-08-03

## Opening story

I used this technical challenge as an opportunity to solve a real problem that I
already care about.

I provide technical support to a cultural hub called **UBEMTEM**. UBEMTEM uses
Libib.com to manage its book collection. Libib is useful as a cataloging tool,
but it does not give us enough flexibility to design and evolve the public
catalog page around UBEMTEM's identity, accessibility needs, and future content
plans.

So I decided to treat the challenge not only as a CRUD exercise, but as the
first version of our own catalog application. The system imports data exported
from Libib, normalizes it into a relational model, and gives catalog managers an
administrative interface while keeping public publication explicit and safe.

That real context influenced several decisions:

- I preserved the original `creators` text because cultural attribution matters.
- I kept one primary Author per Book to satisfy the challenge requirement, but
  left room for future contributor modeling.
- I used `Not Identified` for missing authors instead of inventing names.
- I used `Unclassified` for Genre because the source data does not contain a
  trustworthy classification.
- I defaulted imported books to `publishOnSite = false` so unreviewed records do
  not become public accidentally.
- I added cover enrichment from the existing enriched Libib JSON first, and only
  then Google Books as an optional fallback.

## Current production demo

The deployed demo uses:

- frontend: `https://biblio.ubemtem.org`
- API: `https://books-lib-yy5q.onrender.com`
- database: Render PostgreSQL
- DNS: Squarespace-managed `ubemtem.org`, with `biblio` pointing to Vercel

The main UBEMTEM website is managed from a separate repository, so the admin app
uses a subdomain instead of being mounted at `/biblio-admin`.

## Two-minute architecture explanation

Books Library is a small modular monolith with a React/Vite frontend, an
ASP.NET Core API, and PostgreSQL.

The browser never talks directly to the database. The API is the boundary for
authorization, validation, transactions, import compatibility, and public versus
administrative data exposure.

On the backend, I organized the code by feature: Books, Authors, Genres,
Identity, Audit, and import support. This keeps the code easy to review because
each feature owns its endpoint contracts, validation, domain behavior, and
tests. I avoided a heavy multi-project architecture because the domain is small
and the timebox rewards coherent delivery over ceremony.

The relational model is intentionally simple:

- many Books to one Author;
- many Books to one Genre;
- soft deletion for catalog records;
- audit entries for delete operations;
- users for catalog managers.

Administrative write endpoints require JWT authentication and a policy that
rejects temporary bootstrap credentials until the password has been changed. The
public catalog is a separate anonymous read path that only returns active books
marked for publication.

The frontend uses React with TanStack Query and Axios. Server state remains in
the API, while local UI state handles forms, filters, pagination, language, and
session behavior. The UI supports English, Portuguese, and Spanish, and it
remembers the selected language in a cookie.

## Demo setup

For the production demo, open:

- Web app: `https://biblio.ubemtem.org`
- API live health: `https://books-lib-yy5q.onrender.com/health/live`
- API ready health: `https://books-lib-yy5q.onrender.com/health/ready`

For a local demo, start from a clean local run:

```bash
BOOKSLIB_BOOTSTRAP_EMAIL=admin@bookslib.local \
BOOKSLIB_BOOTSTRAP_PASSWORD=ChangeMe123! \
BOOKSLIB_JWT_SIGNING_KEY=local-development-signing-key-change-before-production-12345 \
docker compose up --build
```

Open:

- Web app: `http://localhost:5173`
- Swagger: `http://localhost:5080/swagger`
- API health: `http://localhost:5080/health/ready`

Seed the catalog from the CSV export:

```bash
.venv/bin/python scripts/import_catalog_csv.py --fetch-covers --report /tmp/booklib-import-report.json
```

If the database was already seeded, the importer is rerunnable. It reports the
rows as duplicates and updates missing covers when possible instead of inserting
duplicate books.

## Suggested demo flow

1. Show `/health/ready` returning healthy.
2. Open the SPA and sign in with the configured admin account.
3. Show the mandatory password-change screen if the bootstrap password is still
   active.
4. Change the password and enter the admin catalog.
5. Show the book list:
   - cover thumbnails;
   - pagination;
   - search;
   - Author and Genre filters;
   - multilingual UI selector.
6. Open or edit one imported book and point out:
   - Author relation;
   - Genre fallback;
   - publication flag;
   - cover URL.
7. Create a new Author or Genre and show validation behavior.
8. Explain public catalog behavior:
   - imported books default to unpublished;
   - only active published books with active Author and Genre are public.
9. Show the import report:
   - rows read;
   - duplicate handling;
   - fallback counts;
   - cover enrichment counts.

## Trade-offs to explain

### Why Vercel for frontend and Render for backend?

The frontend is a static Vite SPA, so Vercel gives a simple CDN-oriented deploy
and custom-domain workflow. The backend is a Dockerized .NET API with
PostgreSQL, health checks, migrations, and secret configuration, so Render is a
better fit for that workload. The trade-off is operating two providers; the
benefit is that each part runs in the environment with the least friction.

### Why a modular monolith?

The team and domain are small. A modular monolith gives clean ownership without
the operational overhead of microservices. If UBEMTEM later needs independent
content modules or heavier public traffic, we can extract services from proven
module boundaries.

### Why PostgreSQL?

The data is relational: Books require Authors and Genres, soft deletion must be
queryable, and the import process benefits from constraints and transactions.
PostgreSQL is robust, inexpensive, and supported by the chosen hosting path.

### Why not model multiple authors now?

The challenge requires one Author per Book. The Libib export has a `creators`
field that can include multiple credits, but it is free text and not safe to
split automatically. I preserved it as `creatorCredit` and modeled one primary
Author. A future contributor model can be added after stakeholder review.

### Why imported books are unpublished by default?

Publication is an editorial decision. The source export is useful, but it may
contain incomplete authorship, unclassified genres, and descriptions copied from
external sources. Defaulting to unpublished prevents accidental public exposure.

### Why local JSON before Google Books for covers?

The enriched Libib JSON is closer to the real source and already contains cover
URLs curated for this collection. Google Books is helpful as a fallback, but it
is external, slower, and may return incomplete or mismatched metadata.

### Why store JWT in localStorage?

For the challenge, it keeps the SPA simple and demonstrable. For production, I
would revisit this with a threat model and likely consider secure httpOnly
cookies, CSRF controls, refresh-token rotation, and logout/revocation semantics.

## Quality and evidence

Automated checks:

```bash
dotnet test BooksLib.sln
python3 -m unittest tests/import_catalog_csv_tests.py
npm --prefix apps/web run lint
npm --prefix apps/web run build
npm --prefix apps/web audit --omit=dev
git diff --check
```

Covered by tests:

- domain normalization;
- field validation;
- system Author and Genre rules;
- Book creation/update behavior;
- audit entry creation;
- login failure;
- unauthorized write;
- forbidden write with expired bootstrap credential;
- password change unlocking writes;
- expired JWT rejection;
- import normalization, duplicates, fallbacks, safe publication default, and
  cover lookup selection.

## Known limitations and next steps

- Server-side validation messages are not localized yet.
- No rate limiting is applied to login.
- JWT revocation and refresh-token rotation are not implemented.
- Audit entries are written, but there is no admin audit UI yet.
- Search uses normalized `Contains`; larger catalogs should move to PostgreSQL
  full-text or trigram search.
- No automated axe or Lighthouse report is committed.
- Logs are written to stdout/stderr only; production persistence depends on the
  hosting provider's log stream unless a sink is added.

## Closing statement

The important part of this solution is that it satisfies the technical
challenge while being grounded in a real operational need. It is small enough to
understand, but already has the boundaries UBEMTEM would need next: public
catalog design freedom, safe imports, explicit publication, multilingual
administration, and a clear path toward richer cultural-collection features.
