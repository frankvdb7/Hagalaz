# Checksum endpoint DTOs
## Why
ChecksumEndpoints still contains nested response records.
## What Changes
Extract GetChecksumTableDto and ChecksumTableEntryDto into Checksums/Dtos with matching namespaces, preserving the JSON contract.
## Acceptance criteria
1. The checksum route uses dedicated DTOs without exposing cache model types.
2. Ordered index IDs, signed CRC values, versions and base64 digests remain unchanged for populated and empty indexes; service tests/build pass.
## Non-goals
No checksum algorithm, route, cache ownership or client API changes. No admin consumer uses this route.
## Capabilities
### New Capabilities
- `checksum-contracts`: explicit checksum table REST DTOs.
### Modified Capabilities
None.
## Impact
Checksum DTO files, endpoint mapping and read-only HTTP regression tests.
