## Context

`NpcBuilder` already creates the per-NPC scope and uses
`INpcScriptActivator.Create(Type, INpc)` to provide the runtime owner. The
plugin startup scan bypasses that boundary by registering each concrete NPC
script with Microsoft DI, which validates constructors before an owner exists.

## Decision

Remove only the concrete NPC-script scan and the ordinary DI registrations for
the default NPC and familiar scripts. Expose the assemblies loaded by the
existing plugin host through infrastructure assembly descriptors and make
`NpcScriptMetaDataFactory` flatten concrete `INpcScript` types from those
assemblies through the existing `IServiceDescriptorProvider`, deduplicating by
type. The plugin host owns the assembly boundary, so unrelated loaded
assemblies and NPC-script service descriptors cannot become a second source
accidentally.

`NpcScriptActivator` remains the sole owner-aware construction path. Other
script categories continue using their current registration and metadata
mechanisms.

## Risks

Assembly type enumeration can encounter a partially loadable assembly, so the
metadata factory retains the loadable types from `ReflectionTypeLoadException`
and logs the loader exception. Types without `NpcScriptMetaDataAttribute`
remain ignored, as before.
