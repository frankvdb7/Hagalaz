- [x] 1. Add pending/prepared map-part buffers with a synchronized prepare and
      completion boundary.
- [x] 2. Add regression coverage for frozen ticks, cutoff preservation,
      duplicate filtering, same-buffer fan-out, and concurrent queueing.
- [x] 2.1 Add failure-isolation coverage proving later regions and characters
      continue and pending updates survive an aborted client phase.
- [x] 2.2 Keep client-update preparation state local to the worker, skip
      delivery for regions whose preparation failed, and always run reset in
      the worker's `finally` boundary.
- [x] 2.3 Narrow region client exception handling to connection failures at the
      character boundary; let unexpected programming and NPC-update failures
      propagate, with regression coverage for both behaviors.
- [ ] 3. Run focused tests, the affected build, strict OpenSpec validation, and
      repeat the real client world-entry action.
