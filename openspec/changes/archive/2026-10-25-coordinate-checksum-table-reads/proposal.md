# Proposal

## Why

`CacheApi.CreateChecksumTable` reads reference-table files while local archive mutations can replace archive and reference-table data under the cache write lock. Without taking the matching read lock, checksum generation can observe a mutation partway through.

## What Changes

- Hold the existing cache read lock for the complete checksum-table calculation.
- Add a concurrency regression using the real cache writer and file-store boundary.

## In Scope

Coordinate checksum generation with writes made through the same `CacheApi` instance.

## Non-Goals

Do not add another lock or cache, and do not coordinate changes made by other processes.

## Capabilities

### Modified Capabilities

- `cache-archive-member-snapshots`: Checksum generation is coherent with local writes.

## Acceptance Criteria

1. A local archive mutation cannot persist while checksum generation holds the cache read lock.
2. The generated checksum reflects the reference-table bytes observed before the waiting write.

## Stop Conditions

Stop if the existing cache read/write lock cannot safely cover checksum generation without recursion.
