# Product Definition

**Status:** Draft for stakeholder validation<br>
**Last updated:** 2026-07-29<br>
**Product:** Books Library

## 1. Problem and outcome

Small cultural organizations often keep catalog information in spreadsheets or
disconnected documents. That makes records hard to find, inconsistent, and
difficult to reuse in a public catalog. Books Library will provide one reliable
place for authorized staff to maintain works, their authors, and their genres.

The first outcome is not “three CRUD screens.” It is a trustworthy catalog that
answers: **what works do we hold, who created them, and how are they
classified?** The model should later accommodate UBEMTEM's local authors,
cultural works, and documents without pretending those future workflows are
already known.

## 2. Users and roles

| Persona | Need | MVP access |
| --- | --- | --- |
| Catalog manager | Find and maintain accurate catalog records | Authenticated read/write |
| Visitor/integrator | Browse catalog data | To be confirmed: public read or authenticated read |
| Administrator | Manage user access | Seeded bootstrap account only; user administration is deferred |

## 3. Ubiquitous language

- **Book:** a cataloged work with a title and optional publication metadata.
- **Author:** a person credited for one or more books.
- **Genre:** a controlled classification applied to books.
- **Catalog:** the complete set of managed records and relationships.
- **Catalog manager:** an authenticated person allowed to change the catalog.

“Book” is retained for the challenge. A premature generic `ContentItem` is
rejected: its fields and invariants would be guesses. Future content types can
be introduced behind a shared catalog concept when UBEMTEM requirements are
known.

## 4. Assumed MVP scope

These are explicit assumptions, not silently invented requirements:

1. Authenticate a catalog manager with email and password and issue a JWT.
2. List, view, create, update, and delete books, authors, and genres.
3. Associate a book with one or more authors and one or more genres.
4. Search books by title and filter by author or genre.
5. Paginate collection endpoints with deterministic ordering.
6. Validate input at the API boundary and return RFC 7807 Problem Details.
7. Document endpoints through OpenAPI/Swagger.
8. Expose liveness and database-readiness health checks.
9. Provide a responsive administration UI with loading, empty, error, and
   success states.

## 5. Initial domain rules (require confirmation)

- Identifiers are server-generated UUIDs and are never reused.
- Titles and display names are required after trimming whitespace.
- An author or genre referenced by a book must exist.
- Duplicate author/genre links on one book are invalid.
- Deleting a referenced author or genre returns a conflict rather than silently
  changing books.
- Email comparison is case-insensitive.
- Pagination has a bounded page size; the proposed default is 20 and maximum is
  100.

## 6. MVP acceptance criteria

- An authenticated manager can complete all assumed catalog operations from the
  UI without direct database access.
- Invalid requests use a consistent Problem Details shape with field errors and
  a trace identifier.
- Unauthorized writes return `401`; forbidden actions return `403` when roles
  are introduced.
- List endpoints are paginated and do not produce EF Core N+1 queries.
- Concurrent or invalid relationship changes cannot leave partial data.
- API integration tests cover the happy path, validation, authentication,
  missing resources, and relationship conflicts.
- A new developer can run the whole system with documented Docker Compose
  commands once implementation begins.
- No secret is committed; production configuration comes from environment
  variables.

## 7. Non-functional requirements

- **Maintainability:** feature-oriented modules and plain code over speculative
  abstractions.
- **Security:** hashed passwords, short-lived signed tokens, restricted CORS,
  sanitized logs, and no secrets or credentials in responses.
- **Reliability:** transactional writes, database constraints, global exception
  handling, readiness checks, and graceful startup failure.
- **Observability:** structured request logs with correlation/trace identifiers;
  never log passwords or JWTs.
- **Accessibility:** keyboard navigation, associated labels, visible focus, and
  semantic status feedback.
- **Operations:** one API service, one static web deployment, and managed
  PostgreSQL to minimize solo-developer cost.

## 8. Out of scope for the first release

Loans, inventory copies, cover uploads, ISBN-provider integration, password
reset, self-registration, fine-grained permissions, audit history, localization,
offline support, event buses, microservices, and a generic CMS. Each may become
a later slice after a validated need.

## 9. Success measures

- A manager can add a valid book with existing or new classifications in under
  two minutes during a usability walkthrough.
- All MVP acceptance scenarios pass in CI.
- A deployment and rollback can be performed from repository documentation.
- The architecture can be explained in a ten-minute English interview segment
  without relying on framework jargon.

## 10. Questions blocking implementation

1. What is the original challenge statement and its exact field, endpoint,
   screen, and delivery requirements?
2. Is reading public, or must every operation require authentication?
3. Are author and genre relationships one-to-many or many-to-many according to
   the challenge?
4. Which book metadata is mandatory (ISBN, synopsis, publication date,
   publisher, language, cover)?
5. Is hard deletion required, or must records be archived/soft-deleted?
6. Is authentication part of the assessed scope, and how should the initial
   user be provisioned?
7. Are there prescribed endpoint names, response examples, UX references, or
   evaluation scripts?

Implementation starts after these answers are recorded here. This gate avoids
building polished software that fails the actual challenge contract.
