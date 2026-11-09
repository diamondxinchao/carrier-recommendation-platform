# Verification and measurements

## Check suites

| Suite | Main checks |
| --- | --- |
| `Carrier.CoreChecks` | 13 dependency-free checks: ranking bounds/cap, 100 score agreement scenarios, invalid history, lane normalization, validation boundaries, application port behavior, architecture references |
| xUnit SQLite repository | Same-lane correctness, equipment/capacity/date/rate eligibility, all-candidate ranking before paging, tie stability, inclusive availability, empty pages, no tracked reads |
| xUnit HTTP | Successful recommendations, score payload, timing headers, validation problems, invalid query/enum, missing carrier, readiness |
| xUnit SQL Server | Actual SQL Server filtering/ranking/pagination translation against a disposable database |
| Vitest | API failure messages, damaged local storage, deleted saved carriers, empty-state reset, error retry |
| Playwright | Discovery → explanation → save → shortlist → refresh → remove, impossible rate, reset, desktop and mobile overflow check |

## Verification on the implementation machine

The Domain, Application, and core-check projects compiled with .NET SDK 10.0.402 with **zero warnings and errors**, and **13/13 core checks passed**.

All 10 TypeScript/TSX files passed a Babel syntax parse using the bundled tooling. JSON and project XML parsed successfully, the benchmark script passed Python syntax compilation, and the staged changes passed Git's whitespace check. Syntax parsing does not replace TypeScript type checking, React execution, or browser validation.

Package downloads on the implementation machine are blocked by session network restrictions, so full verification runs on GitHub-hosted runners. Check the [latest Quality checks results](https://github.com/diamondxinchao/carrier-recommendation-platform/actions/workflows/ci.yml) for the backend build and HTTP/SQLite tests, SQL Server migration/query test, frontend build/Vitest, and desktop/mobile Playwright tests. No local performance or accuracy number is claimed.

To run only the dependency-free checks without contacting package services, use an empty local NuGet feed for restore:

```sh
mkdir -p artifacts/empty-feed
dotnet restore tests/Carrier.CoreChecks --source ./artifacts/empty-feed -p:NuGetAudit=false
dotnet run --project tests/Carrier.CoreChecks --no-restore
```

If an environment blocks build-server IPC, add `-m:1 -p:UseSharedCompilation=false -nodeReuse:false` to `dotnet build`, then run the compiled check assembly directly. This does not disable code checks; it avoids shared compiler processes.

## Measuring latency honestly

With the API running:

```sh
python3 scripts/benchmark.py --requests 30 --concurrency 2
```

The script performs five warmups and reports the timestamp, request count, concurrency, errors, eligible result counts, mean, P50 and nearest-rank P95. It measures one fixed synthetic shipment over the local demo. Keep runs below the configured per-IP limit or wait for a new minute window. Preserve the output together with database size/provider, hardware, release commit, and deployment mode.

For a before/after comparison, use the same dataset, request mix, concurrency, build configuration and warmup. Keep expected carrier IDs/ranking judgments as a guardrail. Do not infer a production improvement from this small dataset.

## Quality and rollout

Before a real launch, Product should supply independently judged shipment/carrier cases. Track eligibility recall before ranking quality: an incorrectly excluded carrier cannot be recovered by a better ranker. Compare top-K precision/recall or approved pairwise preferences against the prior implementation; include sparse-history, fully booked, capacity-boundary and unavailable-route cases.

The feature flag provides an immediate ordering fallback. Define latency/error/relevance thresholds using measured baselines, then use a controlled rollout with explicit experiment assignment and selection/contact event definitions. Neither event collection nor A/B assignment is simulated by this demo.
