- [x] 1. Remove ordinary DI registration of owner-aware NPC scripts.
- [x] 2. Expose plugin assemblies through the existing service descriptor
      provider and preserve NPC metadata discovery from their usable types.
- [x] 2.1 Keep NPC script discovery separate from NPC script service
      descriptors; retain the descriptor provider for other script categories
      and infrastructure assembly descriptors.
- [x] 3. Add focused regression coverage for registration and discovery.
- [x] 3.1 Deduplicate plugin assemblies and script types, ignore abstract and
      metadata-less scripts, and retain usable types after partial load errors.
- [x] 3.2 Keep `NpcScriptActivator` as the runtime owner-aware construction
      boundary and validate the focused provider with scoped DI validation.
- [ ] 4. Run focused tests, affected builds, strict OpenSpec validation, and
      the real Aspire startup check.
