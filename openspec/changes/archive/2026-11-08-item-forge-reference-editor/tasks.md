# Tasks

## 1. Cache mutations
- [x] 1.1 Load and save the full ItemType using the current codec, retaining unmodeled opcode bytes; test validation, extended model IDs and template/counterpart round trips (AC1).
- [x] 1.2 Add exact append-copy through existing write ownership; test same/new archive boundaries, unchanged source and subsequent reads, and expose protected save/duplicate endpoints (AC2).

## 2. Admin reference implementation
- [x] 2.1 Implement measured header/editor/inspector layout using existing assets and controls; wire save, duplicate, dirty guards, source refresh and technical details; test happy/error/validation/navigation behavior (AC3).

## 3. Integration
- [x] 3.1 Run focused backend/frontend regressions and both application builds, rebuild cache, review ownership and validate OpenSpec. Record browser waiver and blocked visual QA without claiming visual fidelity (AC4).

- [x] 3.2 Move item-specific allocation out of CacheApi; retain atomic generic member copy and verify duplicate boundary/concurrency regressions (AC2).

- [x] 3.3 Replace the destination callback with a direct archive append-copy operation using an explicit member limit; retain boundary and concurrency behavior (AC2).

- [x] 3.4 Remove the source-specific codec overload; carry JSON-ignored preserved payloads on ItemType and verify ordinary encoding, API preservation and JSON isolation (AC1).
