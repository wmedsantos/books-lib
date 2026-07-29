# Day 2 — Authors, books, and core relationships

**Date:** 2026-07-29

## Objective

Complete the catalog-management core by adding Authors and Books end to end,
including required Book relationships, search, filters, pagination, and focused
tests.

## Acceptance criteria

- Authors can be created, listed/searched, edited, and soft-deleted through the
  API and SPA.
- Books can be created, listed/searched, edited, and soft-deleted through the
  API and SPA.
- Every Book references one active Author and one active Genre.
- Book lists display Author and Genre names without an EF Core N+1 query.
- Active Authors and Genres with active Books cannot be deleted.
- The system Author `Not Identified` and system Genre `Unclassified` cannot be
  renamed or deleted through normal management operations.
- Main quality commands pass locally.

## Checklist

- [x] Author database model and bootstrap fallback added.
- [x] Author API implemented.
- [x] Author UI implemented.
- [x] Book database model and migration added.
- [x] Book API implemented.
- [x] Book UI implemented.
- [x] Book search and Author/Genre filters implemented.
- [x] Relationship conflict rules implemented.
- [x] Focused domain tests added.
- [x] README and backlog updated.

## Development notes

Authentication, public catalog projection, audit entries, and JSON import remain
Day 3/delivery-hardening work. The Day 2 delete operations already enforce the
relationship conflicts that those later slices depend on.
