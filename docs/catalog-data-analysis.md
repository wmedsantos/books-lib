# UBEMTEM Catalog Data Analysis

**Status:** Full export profiled<br>
**Sample size:** 213 records from `biblioteca_ubemtem_com_capas_libib_final.json`<br>
**Full-export conclusions:** Available for MVP schema and import-script design

## Observed shape

All 213 records have `item_type = "book"` and the same collection name,
`Biblioteca Ubemtem Vó Esméria`. Values are encoded primarily as strings,
including numeric values such as `copies` and `length`; missing values are
usually empty strings, with a small number of `null` values in Libib-derived
fields. Dates, when present, use `YYYY-MM-DD`. Covers are HTTPS URLs, not
embedded image data.

Fields observed in every record:

```text
item_type, title, creators, first_name, last_name, collection,
ean_isbn13, upc_isbn10, description, publisher, publish_date,
group, tags, notes, price, length, number_of_discs, number_of_players,
age_group, ensemble, aspect_ratio, esrb, rating, review, review_date,
status, began, completed, added, copies, cover_url, libib_title,
libib_author
```

## Dataset findings

- `title` is populated for every row. One normalized title appears twice:
  `O Tupi Que Você Fala`.
- `title` and `libib_title` differ in 48 rows; `title` remains the domain title
  and `libib_title` remains import provenance only.
- `creators` can contain multiple comma-separated credits, while `libib_author`
  and the split name fields identify only the first person.
- `libib_author` is missing for 3 rows. Import resolves all rows without
  `libib_author` to the controlled fallback Author `Not Identified`.
- `creators` and `libib_author` differ in 88 rows, confirming that
  `creatorCredit` should be preserved separately from the primary Author.
- ISBN-10 and ISBN-13 are each missing in 54 rows. Non-empty ISBNs are unique in
  the export.
- `publisher` is missing in 61 rows, `publish_date` in 126 rows, and `length` in
  134 rows, so they cannot be required.
- All populated dates are valid ISO dates. All populated `copies` and `length`
  values are positive integers. `copies` is `1` for 211 rows and `2` for 2 rows.
- `cover_url` is missing in 1 row. Every populated cover URL uses HTTPS.
- `group` and `tags`, the most plausible classification candidates, are empty in
  all 213 rows. The export therefore provides no defensible Genre mapping.
- Media-specific Libib fields such as discs, players, aspect ratio, and ESRB are
  empty and do not belong in the Book MVP.
- Publication and source-added dates describe different events; validation checks
  syntax but does not impose an ordering rule without a domain requirement.

## Normalization policy

1. Trim strings and convert empty strings to null before validation.
2. Parse numeric strings using invariant culture and reject negative values.
3. Parse ISO dates strictly; retain invalid source rows in an import-error report
   rather than silently changing them.
4. Treat ISBNs as identifiers, never numbers, so leading zeros are preserved.
5. Resolve the primary Author from trimmed `libib_author`; when it is missing,
   use the controlled fallback Author `Not Identified`. Preserve `creators` as a
   display credit when it contains additional contributors.
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

## Import-script implications

- The importer can target the MVP Book schema without adding genre, media, or
  review fields.
- The importer must create or find the `Unclassified` Genre and assign it to all
  imported books unless a trusted classification source is added later.
- Rows missing `libib_author` must use the `Not Identified` Author and be counted
  in the import report.
- Duplicate source rows with the same normalized title and same populated ISBN
  should discard the later row and report it. Duplicate normalized titles with
  different ISBNs should be imported as likely separate editions by appending one
  period to the later title before persistence.
- Because ISBNs are optional, empty ISBNs must not participate in uniqueness
  checks as a shared value.
- `publishOnSite` must default to `false` because the source has no explicit
  publication flag.
