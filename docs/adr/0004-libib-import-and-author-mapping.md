# ADR 0004: Libib import and single-author mapping

- **Status:** Accepted
- **Date:** 2026-07-29

## Context

The challenge requires exactly one Author per Book. The UBEMTEM sample contains
`creators` values with multiple comma-separated people, while `libib_author` and
the split name fields identify only the first person. The sample has no usable
Genre value. Empty source values are encoded as strings, and numeric fields are
also strings.

## Decision

1. Use `libib_author` as the canonical primary Author name for import and make
   the resulting `author_id` required. When `libib_author` is missing, resolve
   the row to the controlled fallback Author `Not Identified`.
2. Preserve `creators` as optional `creator_credit` text when it conveys
   additional source attribution. It is not a second Author relationship.
3. Ignore `first_name`/`last_name` for identity resolution; they are incomplete
   for multi-credit records and names are culturally difficult to split safely.
4. Keep import DTOs separate from domain entities. Normalize empty strings,
   parse dates/numbers, validate, and report row-level errors before persistence.
5. Use the system Genre `Unclassified` when the source record has no Genre. The
   import script creates or resolves this Genre deterministically and reports how
   many records received it; it does not infer a more specific classification.
   Resolve it by immutable system code `unclassified`, and prevent rename or
   deletion through normal Genre operations.
6. Default every imported Book to `publish_on_site = false`.
7. Treat duplicate source rows deterministically. If two rows have the same
   normalized title and the same populated ISBN, discard the later duplicate and
   report it. If the normalized title matches but the ISBN differs, import the
   later row as a likely separate edition by appending one period to the title
   before persistence.

## Rationale

This preserves the challenge's required cardinality and the source's visible
credit without prematurely introducing contributor entities or a many-to-many
Author model. A boundary DTO prevents Libib-specific fields and string encoding
from contaminating the catalog domain. Publication opt-in is the safe default
for a public endpoint. The fallback Author keeps imports deterministic without
inventing a personal name when the source has no author signal.

## Consequences

### Positive

- Domain cardinality remains correct and directly explainable.
- Additional creator attribution is not discarded.
- Import failures can be corrected without partially persisted rows.
- Unknown classification is visible and searchable rather than fabricated.
- Missing authorship is visible and searchable rather than blocking the import.
- Duplicate handling is repeatable and produces a reviewable report.

### Negative

- Additional creators cannot be searched as Authors in the MVP.
- Name-based resolution can merge homonyms or split spelling variants; the
  importer must report proposed matches for review.
- Imported records require later catalog curation to replace `Unclassified`.
- Records assigned to `Not Identified` require later authorship curation.
- Appending a period to title-only duplicate editions is intentionally simple
  but may need richer edition metadata later.

## Alternatives considered

- **Split `creators` on commas into Authors:** rejected because commas may not be
  a reliable person delimiter and it violates the required single-Author model.
- **Discard `creators`:** rejected because it loses attribution visible in the
  source catalog.
- **Create contributor entities now:** deferred under YAGNI; the challenge does
  not require contributor management or search.
- **Infer Genre with keywords or AI:** rejected because unreviewed classification
  would reduce catalog trust and add operational complexity.
- **Reject every record without Genre:** rejected because the current source has
  no Genre data and the import would provide no catalog value.
- **Reject every record without Author:** rejected because the product decision
  is to preserve those catalog entries with the visible `Not Identified`
  fallback.
- **Merge every duplicate title:** rejected because a same-title row with a
  different ISBN may represent a second edition.

## Revisit when

A reliable Genre field, edition metadata, or a validated need to search and
manage all contributors independently appears. If a Genre source is added later,
explicit source values take precedence over the fallback.
