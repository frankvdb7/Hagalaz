## Context

`NpcBuilder` already creates the per-NPC scope and uses
`INpcScriptActivator.Create(Type, INpc)` to provide the runtime owner. The
plugin startup scan bypasses that boundary by registering each concrete NPC
script with Microsoft DI, which validates constructors before an owner exists.

## Decision

Remove only the concrete NPC-script scan and the ordinary DI registrations for
the default NPC and familiar scripts. Register one explicit
`INpcScriptTypeCatalog` for the plugin assembly and update
`NpcScriptMetaDataFactory` to combine its existing service-descriptor types
with concrete `INpcScript` types from all registered catalogs, deduplicating by
type. The catalog owns the assembly boundary, so unrelated loaded assemblies
cannot become script sources accidentally.

`NpcScriptActivator` remains the sole owner-aware construction path. Other
script categories continue using their current registration and metadata
mechanisms.

## Risks

Assembly type enumeration can encounter a partially loadable assembly, so the
catalog will retain the loadable types from `ReflectionTypeLoadException`.
Types without `NpcScriptMetaDataAttribute` remain ignored, as before.
