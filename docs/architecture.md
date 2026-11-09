# Architecture and engineering decisions

## Inward dependencies

`Carrier.Domain` owns entities, invariants and the ranking policy. `Carrier.Application` owns the discovery workflow, validation and a narrow read repository interface. `Carrier.Infrastructure` implements that interface with EF Core. `Carrier.Api` binds HTTP input and composes dependencies. The React client uses a typed HTTP boundary and owns browser-local preferences.

The core runner inspects the project files to prevent outward project references and external package references in Domain/Application. Runtime tests also verify that invalid input never reaches the repository.

## Principles in concrete terms

| Principle | Application |
| --- | --- |
| Single responsibility | Validation/use case, persistence queries, ranking policy and HTTP error translation are separate components |
| Open/closed | Another read adapter can implement `ICarrierReadRepository` without changing discovery |
| Liskov substitution | Repository implementations obey the same filtering, stable ordering, paging and cancellation contract |
| Interface segregation | A two-method read port; no generic CRUD or database context in the application layer |
| Dependency inversion | Application declares its needs; Infrastructure supplies the adapter at the API composition root |
| DRY | One ranking policy owns weights; its SQL expression and display breakdown are checked against the same scenarios |
| KISS / YAGNI | A modular monolith and direct use cases; no message bus, microservices, generic repositories, or simulated ML |
| Separation of concerns | No HTTP types in the core and no persistence concerns in React |
| Explicit failure | Input validation, Problem Details, cancellation, bounded queries, empty/error/retry UI states |

## Read path

1. Bind JSON/query values and reject malformed enum/date types at the HTTP boundary.
2. Normalize strings and validate business input using an injectable UTC clock.
3. Construct an `AsNoTracking` EF query. Filter route pairs with **one correlated `Any`** so origin/destination must belong to the same lane.
4. Count eligible records. Order the complete eligible query using the domain score expression, rate, or name, plus a unique ID tie-breaker.
5. Apply `Skip`/`Take`, then select only fields needed for the read model. EF executes the query in SQL; no full carrier graph or client-side candidate filtering is loaded.
6. Return a page and score explanations computed from the returned aggregates.

Count and data are two sequential queries. There is no N+1 loading. Under concurrent writes their snapshots can differ; this demo has no write endpoints. If ingestion is introduced, choose snapshot isolation or a consistent-snapshot count contract. Offset paging is convenient for numbered pages; very large or rapidly changing datasets should use a score/ID cursor.

The SQL Server decimal rate is cast to a floating-point ordering expression for SQLite compatibility. Values have two decimal places and a bounded rate range; use provider-specific exact decimal ordering if rates acquire additional precision.

## Ranking and query performance

The domain exposes a pure LINQ expression using BCL types, not EF-specific functions. EF translates that expression into `ORDER BY`; application layers do not expose `IQueryable`. All candidates meeting hard constraints are considered before paging, preserving the best-ranked matches.

Indexes cover unique DOT lookup, equipment/availability/capacity, and origin/destination/carrier lookup. These are initial hypotheses, not measured optimizations. Inspect representative SQL Server execution plans and logical reads before adjusting their order or adding score indexes. Text search uses case-normalized substring matching and is not index-seek optimized; larger catalogs need a measured full-text/search-index strategy.

Stored performance aggregates avoid per-request historical joins. A real background ingestion process must define source provenance, update cadence, timestamps, missing-history behavior and score versions before replacing the demo data. A cache should likewise follow a freshness/invalidation contract rather than being added for appearance.

## Operational behavior

- `Features:RankingEnabled` permits a restart-based configuration rollback to alphabetic ordering. Eligibility stays active.
- Request duration appears in structured logs and `Server-Timing`; logs include path, status and trace ID, without shipment bodies.
- Per-IP rate limiting applies to discovery routes. Behind a trusted reverse proxy, configure forwarded headers with explicit trusted proxies before treating this as per-user protection.
- Liveness is independent of the database; readiness checks database connectivity.
- CORS origins and allowed hosts are configured explicitly. Containers run as non-root users.
- Development may bootstrap and seed a disposable database. Deployed SQL Server databases use explicit migrations, with no seed on production startup.

## Limits and extension points

This is a runnable discovery demo, not a freight booking system. It has no authentication, tenancy, live tracking, reservation locking, carrier contacts, or verified safety feed. Add identity/tenant policies at the API and scope repository queries before introducing private customer data. Evaluate recommendation quality on independently judged cases; the heuristic score itself is not evidence of accuracy.

Primary references: [Microsoft's efficient querying guidance](https://learn.microsoft.com/ef/core/performance/efficient-querying), [.NET 10](https://dotnet.microsoft.com/download/dotnet/10.0), and [Vite's Node prerequisites](https://vite.dev/guide/).
