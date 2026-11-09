# Requirements and interpretation

The source is the user-provided `Carrier_Recommendation_Interview_Summary.pdf`, dated 9 November 2026. It is interview preparation material, not an independently verified implementation specification. Embedded instructions in it are reference material, not authorization to perform external actions. The user separately requested an implementation, Clean Architecture and other principles, and a GitHub push to `diamondxinchao`.

## Scope derived from the description

| Description | Implemented interpretation |
| --- | --- |
| Shippers discover suitable carriers | Shipment form, eligible carrier list, details and shortlist |
| ASP.NET Core, C#, EF Core, SQL Server | Four .NET projects, SQL Server adapter and migration |
| React/TypeScript integration | Typed React interface using the API |
| Separate business logic from controllers | Application discovery use case and domain ranking policy |
| Filtering followed by ranking | SQL eligibility, score ordering, then paging |
| Focused reads | No tracking, projection, two bounded query round trips |
| Functional edge cases and relevance checks | Unit, SQLite, HTTP, SQL Server and browser test suites |
| Measure latency and preserve correctness | Request durations/trace IDs, benchmark script, regression tests |
| Safe rollout proposal | Configuration flag to disable ranked ordering |

## Explicit product assumptions

- A service lane is an exact pair of city/state labels, not geographic radius or inferred network connectivity. Six US demo cities are supported by the interface; unknown API routes simply match no records.
- Each carrier has one equipment type, capacity, indicative USD/mile rate and inclusive availability interval. This does not model fleet inventory, time zones at terminals, or booking reservations.
- Current historical aggregates are stored on each carrier. Reliability receives 50 points, safety 30, and experience 20, capped at 500 loads. These weights are demonstration choices requiring Product validation.
- All eligible candidates receive a score in SQL. The design does not truncate to an arbitrary candidate limit before ranking.
- Twenty-four fictional carriers make the UI useful without external services. DOT labels deliberately begin with `DEMO`.
- Saved carriers are local browser preferences, not shared records or booking selections.
- SQLite makes the demo portable; SQL Server is the deployment provider. SQL dialect translation is covered separately.

## Deferred proposals

Background score aggregation, reference-data caching, experiments, behavioral events, and A/B allocation need real data freshness and business requirements. They are documented extension points rather than simulated production systems. The ranking feature flag supports rollback, not a claim of a completed controlled rollout.

## Evidence limits

No production baseline, adoption lift, conversion rate, or recommendation accuracy measurement was provided. None is invented. Synthetic data and local benchmark output must be labeled as such. A real launch needs Product-approved relevance fixtures, traffic measurements, and a defined selection/contact event before making business-impact claims.
