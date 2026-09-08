- [x] 1. Remove ordinary DI registration of owner-aware NPC scripts.
- [x] 2. Register an explicit plugin NPC type catalog and preserve metadata
      discovery from its types.
- [x] 2.1 Remove service-descriptor discovery from
      `NpcScriptMetaDataFactory`; retain the descriptor provider for other
      script categories that still use it.
- [x] 3. Add focused regression coverage for registration and discovery.
- [ ] 4. Run focused tests, affected builds, strict OpenSpec validation, and
      the real Aspire startup check.
