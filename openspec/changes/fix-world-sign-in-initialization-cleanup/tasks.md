- [x] 1. Abort failed post-authentication world initialization and delegate
      cleanup to the normal disconnect/sign-out owner while preserving the
      original exception.
- [x] 2. Add success and failure regression tests for the world sign-in
      consumer.
- [x] 2.1 Add regression coverage for failed world sign-in character ownership
      and release local ownership before exact remote authorization cleanup.
- [x] 2.2 Extract world-session admission reservation, initialization,
      commit, feature publication, and compensation into one focused service
      while keeping authentication and token revocation in the façade.
- [ ] 3. Run focused tests, the affected build, strict OpenSpec validation, and
      repeat the manual client world-entry flow.
