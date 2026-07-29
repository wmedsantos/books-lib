# ADR 0003: Public projection, soft deletion, audit, and bootstrap identity

- **Status:** Accepted
- **Date:** 2026-07-29

## Context

UBEMTEM needs anonymous site access without exposing draft records or the
administrative contract. Catalog records must be recoverable after deletion and
the operation must be attributable. The first manager must be provisioned
without implementing public registration.

## Decision

1. Provide a dedicated anonymous, read-only public catalog endpoint. Its query
   includes only non-deleted books with `publish_on_site = true` whose single
   author and single genre are also active. Return a minimal public DTO.
2. Model Book-to-Author and Book-to-Genre as required many-to-one foreign keys.
3. Soft-delete books, authors, and genres. Normal queries exclude deleted rows.
   Prevent deletion of an author or genre that still has active books.
4. In the delete transaction, append an immutable audit entry containing actor,
   UTC timestamp, operation, entity type, entity ID, and trace ID. Do not store
   JWTs, passwords, or a full sensitive payload.
5. Bootstrap the first manager through a parameterized operational SQL script.
   Supply email and a compatible precomputed password hash at execution time;
   mark the credential expired. After validating the temporary password, login
   grants only a narrowly scoped password-change capability until reset.
6. Never commit the supplied plaintext temporary password or its reusable hash.

## Rationale

A separate public projection makes publication an allow-list and prevents
administrative fields from leaking as that contract evolves. Soft deletion is
worth its query complexity because recovery and operational accountability were
explicit requirements. An audit row in the same transaction gives the required
guarantee without event sourcing. SQL bootstrap is retained as requested, while
runtime parameters keep credentials outside version control.

## Consequences

### Positive

- Anonymous consumers cannot discover drafts through normal public endpoints.
- Accidental catalog deletions are recoverable and attributable.
- Foreign keys directly express the confirmed cardinalities.
- No registration surface is needed for the MVP.

### Negative

- Every relevant query must correctly handle deletion and publication filters.
- Unique constraints must define whether deleted values can be reused.
- Mandatory reset adds an authentication state and dedicated authorization rule.
- Audit data needs retention and access policies.

## Rejected alternatives

- **Reuse administrative DTOs publicly:** rejected due to accidental disclosure
  and tighter coupling.
- **Global EF query filters alone:** rejected as the only safeguard because
  public visibility also depends on publication and related-record state;
  explicit public projections remain reviewable.
- **Hard deletion plus application logs:** rejected because logs do not restore
  data and may not be transactionally coupled to the delete.
- **Commit a fixed SQL password/hash:** rejected because public source code would
  turn a temporary bootstrap mechanism into a known credential.
- **Self-registration:** rejected because it expands attack surface and is not a
  product requirement.

## Follow-up decisions

- Define uniqueness behavior for deleted records after inspecting source data.
- Define token transport, lifetime, and password-change claim design in the
  Identity slice threat model.
- Define audit retention and administrator access before exposing audit queries.
