# UBEMTEM Catalog Data Analysis

**Status:** Provisional sample profile<br>
**Sample size:** 3 records supplied in the project conversation<br>
**Full-export conclusions:** Not yet available

## Observed shape

All three records have `item_type = "book"` and the same collection name. Values
are encoded primarily as strings, including numeric values such as `copies` and
`length`; missing values are empty strings rather than JSON nulls. Dates, when
present, use `YYYY-MM-DD`. Covers are HTTPS URLs, not embedded image data.

Fields observed in every sample record:

```text
item_type, title, creators, first_name, last_name, collection,
ean_isbn13, upc_isbn10, description, publisher, publish_date,
group, tags, notes, price, length, number_of_discs, number_of_players,
age_group, ensemble, aspect_ratio, esrb, rating, review, review_date,
status, began, completed, added, copies, cover_url, libib_title,
libib_author
```

## Sample-level findings

- `title` and `libib_title` are equal in all three examples.
- `creators` can contain multiple comma-separated credits, while `libib_author`
  and the split name fields identify only the first person.
- One of three examples lacks publisher, publication date, and page count, so
  those fields cannot be required.
- All three examples contain ISBN-10, ISBN-13, cover URL, source-added date, and
  a positive copy count. Three records are insufficient to make them required.
- `group` and `tags`, the most plausible classification candidates, are empty in
  all three examples. The sample therefore provides no defensible Genre mapping.
- Media-specific Libib fields such as discs, players, aspect ratio, and ESRB are
  empty and do not belong in the Book MVP without evidence from the full export.
- Publication and source-added dates describe different events; validation checks
  syntax but does not impose an ordering rule without a domain requirement.

## Normalization policy

1. Trim strings and convert empty strings to null before validation.
2. Parse numeric strings using invariant culture and reject negative values.
3. Parse ISO dates strictly; retain invalid source rows in an import-error report
   rather than silently changing them.
4. Treat ISBNs as identifiers, never numbers, so leading zeros are preserved.
5. Resolve the primary Author from trimmed `libib_author`; preserve `creators`
   as a display credit when it contains additional contributors.
6. Do not infer Genre from title, description, or publisher. Assign the explicit
   system Genre `Unclassified` (stable code `unclassified`) when the source has
   no classification and report the fallback count for later curation.
7. Set `publishOnSite = false` on every import unless an explicit trusted source
   value is introduced.
8. Store source provenance sufficient to diagnose an import without coupling the
   domain model to every Libib field.

## Fields excluded from the initial domain model

`number_of_discs`, `number_of_players`, `ensemble`, `aspect_ratio`, `esrb`,
`rating`, `review`, `review_date`, `began`, and `completed` have no demonstrated
Book-management use case. `price`, `age_group`, `status`, `notes`, `group`, and
`tags` remain candidates pending full-export profiling and stakeholder need.

Exclusion is reversible because the raw source file remains the import artifact;
adding nullable fields later is cheaper than maintaining unused fields and UI.

## Full-export checks still required

- total records and distribution by `item_type` and `collection`;
- key presence, type variation, null/empty frequency, and maximum lengths;
- distinct/non-empty `group`, `tags`, and other future Genre candidates; absence
  is handled by the accepted `Unclassified` fallback;
- mismatch frequency among `title`/`libib_title` and
  `creators`/`libib_author`;
- duplicate and invalid ISBNs, normalized titles, and cover URLs;
- invalid dates/numbers and extreme description/credit lengths;
- unique primary authors and spelling/casing variants;
- URL schemes/hosts and broken-cover handling requirements.

No percentage, uniqueness claim, or dataset-wide constraint will be inferred
from three examples.
