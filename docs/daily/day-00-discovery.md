# Day 0 — Discovery and architecture baseline

**Date:** 2026-07-29

## Objective

Turn an empty repository and a technology/quality brief into a reviewable
product and architecture baseline without prematurely implementing an unknown
challenge contract.

## Backlog

1. Inspect repository and available instructions.
2. Describe users, business outcome, ubiquitous language, and assumed MVP.
3. Select the simplest architecture compatible with the stated stack and
   likely evolution.
4. Document major decisions and trade-offs.
5. Sequence delivery as vertical, testable increments.
6. Record questions that must be answered before code scaffolding.

## Acceptance criteria

- Product Definition distinguishes facts, assumptions, exclusions, and open
  questions.
- Architecture explains boundaries, data flow, operations, security, testing,
  and rejected alternatives.
- Important architectural decisions have ADRs.
- Backlog has ordered slices and observable exit conditions.
- No production code is added before the implementation gate is satisfied.

## Checklist

- [x] Repository and instruction files inspected.
- [x] Domain and personas defined.
- [x] Assumed scope and initial invariants documented.
- [x] Non-functional requirements documented.
- [x] Modular-monolith/vertical-slice decision recorded.
- [x] REST/client-state decision recorded.
- [x] Complexity trade-offs and rejected options documented.
- [x] Delivery backlog ordered.
- [x] README entry point created.
- [x] Challenge statement received and requirements extracted.
- [x] Three representative UBEMTEM JSON records received and profiled.
- [ ] Full UBEMTEM export or aggregate profile received.
- [x] Stakeholder decisions recorded for access, relationships, deletion,
  auditing, and bootstrap identity.
- [x] Delivery requirements captured in a traceability matrix.
- [x] Provisional source-to-domain mapping documented.
- [x] Missing-Genre policy confirmed as the controlled `Unclassified` fallback.
- [ ] Dataset-wide distributions confirmed.
- [x] ADR 0004 accepted for source import mapping.
- [ ] Day 1 checklist created immediately before the walking skeleton.

## Review notes

The supplied challenge confirms the delivery shape and three-day timebox. The
sample confirms the core bibliographic fields but reveals two important facts:
`creators` may contain multiple credits while the challenge requires one Author,
and no reliable Genre value is present. ADR 0004 resolves the former without
violating the challenge and resolves the latter with the explicit `Unclassified`
fallback. Full profiling is required before production import execution, not
before the Day 1 walking skeleton.
