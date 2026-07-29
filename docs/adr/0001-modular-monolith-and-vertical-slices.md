# ADR 0001: Modular monolith with vertical slices

- **Status:** Proposed
- **Date:** 2026-07-29

## Context

The product begins with three closely related catalog concepts and one
developer. It must remain easy to present, deploy, and evolve toward a broader
cultural catalog. A traditional layer-first solution can scatter one change
across many projects, while microservices impose distributed-system cost.

## Decision

Build one .NET 8 API deployable as a modular monolith. Establish explicit
modules for Identity, Books, Authors, and Genres, and organize module behavior
as vertical use-case slices. Keep domain rules out of endpoint definitions.
Share only cross-cutting technical capabilities and stable primitives.

Use direct dependency injection and EF Core. Do not add repositories, mediator
libraries, a message bus, or domain events until a concrete use case benefits
from them.

## Consequences

### Positive

- One process, transaction boundary, deployment, and operational playbook.
- A feature can be understood and tested in one locality.
- Module seams allow later extraction without paying distributed-system cost
  today.
- Less ceremony makes architectural intent easier to explain in an interview.

### Negative

- Module boundaries rely partly on conventions and review until architecture
  tests are worthwhile.
- A shared database can tempt cross-module coupling.
- Independent module scaling is unavailable without extraction.

## Alternatives considered

- **Microservices:** rejected because team topology, scale, and availability do
  not require independent services.
- **Layered Clean Architecture projects:** rejected for the MVP because the
  extra projects and mapping layers would mostly contain pass-through code.
- **Unstructured minimal API:** rejected because it would make future ownership
  and navigation degrade as use cases grow.

## Revisit when

A module requires independent availability/scaling, is owned by another team,
or needs a genuinely different persistence lifecycle; or when unwanted module
coupling repeatedly survives code review.
