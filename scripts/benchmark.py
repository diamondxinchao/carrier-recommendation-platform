#!/usr/bin/env python3
"""Measure a running demo. Results describe this run only, never a historical improvement."""
import argparse
import concurrent.futures
import datetime as dt
import json
import math
import statistics
import time
import urllib.error
import urllib.request

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--url', default='http://localhost:5080')
parser.add_argument('--requests', type=int, default=30)
parser.add_argument('--concurrency', type=int, default=2)
args = parser.parse_args()
if not 1 <= args.requests <= 100 or not 1 <= args.concurrency <= 10:
    parser.error('Use 1-100 requests and 1-10 concurrent workers (API limit: 120/minute/IP).')
body = json.dumps(dict(origin='Chicago, IL', destination='Dallas, TX', equipment='dryVan',
                       pickupDate=dt.datetime.now(dt.timezone.utc).date().isoformat(), weightLb=20000)).encode()

def request(_):
    started = time.perf_counter()
    req = urllib.request.Request(args.url.rstrip('/') + '/api/recommendations', data=body,
                                 headers={'Content-Type': 'application/json'})
    try:
        with urllib.request.urlopen(req, timeout=30) as response:
            result = json.load(response)
        return (time.perf_counter() - started) * 1000, result['totalCount'], None
    except (urllib.error.URLError, ValueError, KeyError) as error:
        return (time.perf_counter() - started) * 1000, None, str(error)

for i in range(5):
    _, _, error = request(i)
    if error:
        raise SystemExit('Warmup failed: ' + error)
with concurrent.futures.ThreadPoolExecutor(max_workers=args.concurrency) as pool:
    results = list(pool.map(request, range(args.requests)))
times = sorted(ms for ms, _, error in results if error is None)
def percentile(p):
    return round(times[max(0, math.ceil(len(times) * p) - 1)], 2) if times else None
print(json.dumps(dict(timestamp_utc=dt.datetime.now(dt.timezone.utc).isoformat(), endpoint=args.url + '/api/recommendations',
                     requests=args.requests, concurrency=args.concurrency, warmup=5, errors=sum(e is not None for _, _, e in results),
                     mean_ms=round(statistics.mean(times), 2) if times else None, p50_ms=percentile(.50), p95_ms=percentile(.95),
                     eligible_counts=sorted(set(c for _, c, e in results if e is None)),
                     note='Local synthetic measurement; not a production baseline or business outcome.'), indent=2))
raise SystemExit(1 if any(error for _, _, error in results) else 0)
