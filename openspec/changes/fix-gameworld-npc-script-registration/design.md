## Context

`NpcBuilder` already creates the per-NPC scope and uses
`INpcScriptActivator.Create(Type, INpc)` to provide the runtime owner. The
plugin startup scan bypasses that boundary by registering each concrete NPC
script with Microsoft DI, which validates constructors before an owner exists.

## Decision

Remove only the concrete NPC-script scan and the ordinary DI registrations for
the default NPC and familiar scripts. Register one explicit
`INpcScriptTypeCatalog` for the plugin assembly and make
`NpcScriptMetaDataFactory` flatten concrete `INpcScript` types from all
registered catalogs, deduplicating by type. The catalog owns the assembly
boundary, so unrelated loaded assemblies and ordinary service descriptors
cannot become a second NPC-script source accidentally.

`NpcScriptActivator` remains the sole owner-aware construction path. Other
script categories continue using their current registration and metadata
mechanisms.

## Risks

Assembly type enumeration can encounter a partially loadable assembly, so the
catalog will retain the loadable types from `ReflectionTypeLoadException`.
Types without `NpcScriptMetaDataAttribute` remain ignored, as before.
