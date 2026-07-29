# Challenge Requirements Traceability

**Source:** “Technical Challenge — Senior Software Engineer,” contents supplied
in the project conversation on 2026-07-29. The binary PDF is not committed in
this repository revision, so page numbers are unavailable.

This matrix converts the challenge into verifiable delivery evidence. Status is
`Planned` until implementation or documentation exists; it is not a claim that
the requirement has already been delivered.

| ID | Requirement | Planned evidence | Status |
| --- | --- | --- | --- |
| CH-01 | Deliver a .NET/C# REST API. | `/apps/api`, OpenAPI document, API tests | Planned |
| CH-02 | Deliver a React or Angular SPA. | React application in `/apps/web` | Planned |
| CH-03 | Use SQL Server, PostgreSQL, or MySQL and justify the choice. | PostgreSQL configuration, migrations, Architecture documentation | Planned |
| CH-04 | Create, read/search, update, and delete genres. | Genre API slices, UI flow, automated tests | Planned |
| CH-05 | Create, read/search, update, and delete authors. | Author API slices, UI flow, automated tests | Planned |
| CH-06 | Create, read/search, update, and delete books. | Book API slices, UI flow, automated tests | Planned |
| CH-07 | A genre may have many books; each book has one genre. | Required `books.genre_id` FK, migration, relationship tests | Planned |
| CH-08 | An author may have many books; each book has one author. | Required `books.author_id` FK, migration, relationship tests | Planned |
| CH-09 | Use clear organization and separation of responsibilities. | Vertical feature slices, ADR 0001, architecture review | Planned |
| CH-10 | Handle errors and return consistent HTTP responses. | Global exception mapping, Problem Details contract, API tests | Planned |
| CH-11 | Persist records in the selected relational database. | EF Core PostgreSQL migrations and integration tests | Planned |
| CH-12 | Provide local execution configuration. | Docker Compose, environment example, README commands | Planned |
| CH-13 | Automate tests for the main scenarios. | xUnit/FluentAssertions test projects and documented test command | Planned |
| CH-14 | SPA lists, registers, edits, and removes all record types. | Route-level screens and browser-facing acceptance tests | Planned |
| CH-15 | SPA shows the Book–Author–Genre relationship. | Book list/detail/form behavior | Planned |
| CH-16 | UI is functional, organized, and understandable. | Consistent application shell and documented UX states | Planned |
| CH-17 | README explains how to run the solution. | Root `README.md` local setup section | Planned |
| CH-18 | Document overview, architecture, backend/frontend organization, database decision, and trade-offs. | README, Architecture, ADRs | In progress |
| CH-19 | Document testing, known limitations, and improvements with more time. | README delivery sections | Planned |
| CH-20 | Submit backend, frontend, database setup, documentation, and relevant tests in a repository. | Final repository audit | Planned |
| CH-21 | Be ready to explain structure, decisions, alternatives, trade-offs, evolution, improvements, developer guidance, and review approach in English. | English presentation guide and ADRs | Planned |
| CH-22 | Complete within three calendar days; prefer functional coherence over excessive scope. | Three-day backlog and daily checklists | In progress |

## Coherently selected differentiators

The challenge labels the following as optional differentiators. The governing
project brief requires them, and the architecture includes them with bounded
scope: Docker containerization, API integration tests, pagination and filters,
JWT authentication, Serilog structured logging, global Problem Details,
FluentValidation, health checks, security review, and ADRs.

They must not delay CH-04 through CH-08 or leave the SPA incomplete. If the
three-day timebox becomes constrained, optional polish is reduced before core
behavior or main-scenario tests.

## Evaluation coverage

The final review must explicitly demonstrate the challenge's evaluation areas:
functional implementation, solution architecture, backend and frontend quality,
domain modeling, automated tests, documentation, technical decisions, and Tech
Lead communication. Evidence links will replace `Planned` statuses as slices are
completed.
