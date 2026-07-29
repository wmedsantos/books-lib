# Product Definition

**Status:** Challenge validated; catalog-data analysis pending<br>
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
| Visitor/integrator | Browse the UBEMTEM site catalog | Public read of books explicitly published on the site |
| Administrator | Manage user access | Seeded bootstrap account only; user administration is deferred |

## 3. Ubiquitous language

- **Book:** a cataloged work with a title and optional publication metadata.
- **Author:** the single primary author used by the challenge relationship.
- **Creator credit:** source text that may credit additional writers,
  illustrators, or contributors without changing the challenge's single-author
  relationship.
- **Genre:** a controlled classification applied to books.
- **Catalog:** the complete set of managed records and relationships.
- **Catalog manager:** an authenticated person allowed to change the catalog.

“Book” is retained for the challenge. A premature generic `ContentItem` is
rejected: its fields and invariants would be guesses. Future content types can
be introduced behind a shared catalog concept when UBEMTEM requirements are
known.

## 4. Required challenge scope

The supplied challenge statement requires:

1. A .NET/C# REST API, React SPA, and relational database.
2. Create, read/search, update, and delete books, authors, and genres through the
   API.
3. List, register, edit, and remove all three record types through the SPA, and
   show each book's author and genre relationship.
4. Associate each book with exactly one author and exactly one genre; an author
   or genre may be associated with many books.
5. Provide clear organization, separation of responsibilities, error handling,
   consistent HTTP responses, relational persistence, local configuration, and
   automated tests for the main scenarios.
6. Provide a README with execution and database-setup instructions plus concise
   architecture, organization, database, trade-off, testing, limitations, and
   future-improvement documentation.

The product brief adds the following required case-study capabilities:

7. Authenticate a catalog manager with email and password and issue a JWT.
8. Use soft deletion for books, authors, and genres.
9. Search books by title and filter by author or genre.
10. Paginate collection endpoints with deterministic ordering.
11. Validate input at the API boundary and return RFC 7807 Problem Details.
12. Document endpoints through OpenAPI/Swagger.
13. Expose liveness and database-readiness health checks.
14. Expose a public, read-only catalog containing only books for which
   `publishOnSite` is `true`.
15. Provide a responsive administration UI with loading, empty, error, and
   success states.

The challenge classifies authentication, containerization, integration tests,
pagination/filtering/sorting, structured logs, global errors, ADRs, validation,
security, and observability as differentiators rather than mandatory checklist
items. They are included selectively here because the governing product brief
requires them and they provide coherent operational value.

## 5. Domain rules

- Identifiers are server-generated UUIDs and are never reused.
- Titles and display names are required after trimming whitespace.
- Every book references one existing, active author and one existing, active
  genre.
- `Unclassified` is the controlled fallback Genre for imported source records
  without classification. It is not guessed from free text and can be replaced
  later by a catalog manager.
- The fallback is identified by the stable system code `unclassified`, not by a
  display-name comparison, and cannot be renamed or deleted.
- Books, authors, and genres are soft-deleted. Delete operations record actor,
  timestamp, entity type, entity ID, and operation in an audit log.
- An author or genre with active books cannot be deleted; the API returns a
  conflict instead of creating catalog records that cannot be administered.
- Soft-deleted records are excluded from normal administrative searches and all
  public responses.
- Only active books with `publishOnSite = true` are visible through the public
  catalog endpoint. Their author and genre must also be active.
- Email comparison is case-insensitive.
- Pagination has a bounded page size; the proposed default is 20 and maximum is
  100.

## 5.1 Provisional Book fields from the supplied sample

The three supplied Libib records justify the following initial mapping. It
remains provisional until the full export is profiled:

| Domain field | Source | Requirement | Notes |
| --- | --- | --- | --- |
| `title` | `title` | Required | Preserve display casing; validate trimmed value. |
| `authorId` | resolved from `libib_author` | Required | Canonical single primary author required by the challenge. |
| `creatorCredit` | `creators` | Optional | Preserve additional source credits without modeling a many-to-many relationship. |
| `genreId` | system fallback | Required | Resolve the `Unclassified` Genre because the source has no classification. |
| `isbn13` | `ean_isbn13` | Optional | Store as text; validate checksum only when populated. |
| `isbn10` | `upc_isbn10` | Optional | Store as text; validate checksum only when populated. |
| `description` | `description` | Optional | Long-form text. |
| `publisher` | `publisher` | Optional | Empty strings normalize to null. |
| `publishedOn` | `publish_date` | Optional | ISO date when valid; the sample contains missing values. |
| `pageCount` | `length` | Optional | Positive integer when the item is a book. |
| `copyCount` | `copies` | Required | Positive integer; represents holdings, not separate inventory records in MVP. |
| `coverUrl` | `cover_url` | Optional | HTTPS URL; remote host availability is not controlled by this system. |
| `collectionName` | `collection` | Optional import provenance | Useful for later multi-collection support, not a separate MVP aggregate. |
| `sourceAddedOn` | `added` | Optional import provenance | Source date, distinct from application audit timestamps. |
| `publishOnSite` | no source field | Required, default `false` | Publication is opt-in; imported records must never become public implicitly. |

`libib_title` duplicates `title` in the sample and is not a second domain field.
`first_name` and `last_name` are not authoritative for the full creator credit:
the first sample has two creators but only the first person's split name. The
application derives the primary Author display name from `libib_author` and
keeps the original `creators` value as `creatorCredit`.

## 6. MVP acceptance criteria

- An authenticated manager can complete all catalog operations from the
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
- Anonymous callers can read the public catalog but cannot access unpublished or
  soft-deleted books, including by guessing an identifier.
- The bootstrap user must change the temporary password on first login before
  accessing catalog-management operations.

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

Loans, inventory copies, ISBN-provider integration, self-registration,
fine-grained permissions, full record-version history, localization,
offline support, event buses, microservices, and a generic CMS. Each may become
a later slice after a validated need.

## 9. Success measures

- A manager can add a valid book with existing or new classifications in under
  two minutes during a usability walkthrough.
- All MVP acceptance scenarios pass in CI.
- A deployment and rollback can be performed from repository documentation.
- The architecture can be explained in a ten-minute English interview segment
  without relying on framework jargon.

## 10. Confirmed decisions

1. The challenge prescribes capabilities but no field-level contracts or screen
   designs. The supplied challenge text has been captured in the
   [requirements traceability matrix](requirements-traceability.md); the UBEMTEM
   JSON export remains the source for actual catalog fields.
2. Administrative operations require authentication. A separate anonymous,
   read-only endpoint exposes only active books marked `publishOnSite = true`.
3. Relationships are many books to one author and many books to one genre. Each
   book has exactly one of each.
4. Book field discovery must use the supplied UBEMTEM JSON export rather than an
   invented schema.
5. Delete means soft delete and must produce an audit record.
6. A bootstrap manager is inserted through an operational SQL script, with the
   supplied temporary credential already expired so the first successful login
   can only proceed to password change. The repository will not contain the
   plaintext password or a reusable production password hash.
7. Endpoint names, response examples, UX references, and evaluator scripts are
   not prescribed.

## 11. Timebox and prioritization

The challenge timebox is three calendar days. A functional, coherent solution
is explicitly preferred over a perfect or overly comprehensive one. Delivery
therefore prioritizes, in order:

1. correct domain cardinalities and relational persistence;
2. complete API and SPA management flows;
3. main-scenario automated tests and reliable local setup;
4. concise decision and trade-off documentation;
5. required case-study differentiators, implemented without displacing the core
   challenge behavior.

## 12. Remaining blocker

The challenge and a three-record JSON sample have been supplied in the project
conversation. This supports a provisional schema, documented in
[Sample data analysis](catalog-data-analysis.md), but not dataset-wide
conclusions. Before executing the importer against production catalog data, the
full export or an aggregate profile must still establish:

- inventory JSON keys, types, nullability, value ranges, duplicates, and cover
  representation without publishing personal or sensitive data;
- how often `creators` and `libib_author` disagree and whether any primary author
  is missing;
- whether ISBNs, titles, cover URLs, dates, and numeric strings are malformed or
  duplicated;
- whether non-book `item_type` values exist and must be rejected.

The walking skeleton, Genre/Author management, and Book CRUD no longer depend on
this profile. The import script can use `Unclassified`; full profiling is still
required before executing it against the complete source so malformed or
duplicate rows are handled deliberately.
