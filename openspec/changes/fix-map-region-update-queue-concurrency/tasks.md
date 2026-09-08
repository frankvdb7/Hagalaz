- [x] 1. Add pending/prepared map-part buffers with a synchronized prepare and
      completion boundary.
- [x] 2. Add regression coverage for frozen ticks, cutoff preservation,
      duplicate filtering, same-buffer fan-out, and concurrent queueing.
- [x] 2.1 Add failure-isolation coverage proving later regions and characters
      continue and pending updates survive an aborted client phase.
- [ ] 3. Run focused tests, the affected build, strict OpenSpec validation, and
      repeat the real client world-entry action.
