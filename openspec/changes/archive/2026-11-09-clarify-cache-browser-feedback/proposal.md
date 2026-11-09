# Clear Cache Studio feedback and discovery

## Goal
Make the existing cache browser communicate requests and supported entry points accurately (approved supervisor checkpoint D3 + D4).

## Acceptance criteria
- AC1: Loading, request error, empty results and populated results are exclusive within the list card. Delayed requests never display old rows under a newer applied URL.
- AC2: Retry repeats the same applied kind/query/page without applying draft text, clearing input or dispatching duplicate requests. Archive metadata failures are retryable.
- AC3: Labels distinguish supported item/NPC name search, exact ID lookup, range browsing, and legacy sprite ID-fragment search. No new backend capabilities.
- AC4: Overview and global page search reach the modern sprite browser; legacy sprite routes keep working.
- AC5: Admin tests, coverage, build, Biome, Fallow, strict OpenSpec and desktop/mobile browser checks are reported to the supervisor.

## Non-goals and stop conditions
No visual redesign, new state owners, backend changes, replacement/removal of legacy tools, World Studio, commits or pushes. Stop for supervisor review after this checkpoint.

## Reuse
Existing Material cards, progress bars and buttons; current route-owned applied parameters, NgRx request state, page search and quick-action links.
