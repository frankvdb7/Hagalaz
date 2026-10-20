## Why

GameWorld fails during service-provider validation because the game-script
plugin registers owner-aware `INpcScript` implementations as ordinary DI
services. Those scripts require an `INpc` constructor argument and must be
created by the existing `NpcScriptActivator` at the NPC composition boundary.

## What changes

- Stop registering concrete owner-aware NPC scripts as ordinary DI services.
- Discover NPC script metadata from plugin assemblies exposed through the
  existing `IServiceDescriptorProvider`.
- Keep default NPC and familiar script types available through their existing
  providers and owner-aware activation path.
- Add regression coverage for plugin registration and metadata discovery.

## Impact

This affects `Hagalaz.Game.Scripts` registration and
`Hagalaz.Services.GameWorld` NPC metadata discovery. It changes no NPC
behavior, script activation contract, network protocol, persistence, or
service lifetime policy.

## Acceptance criteria

- GameWorld can build its service provider with the loaded script plugin.
- No owner-aware concrete `INpcScript` is registered for ordinary DI
  activation.
- Metadata-bearing NPC scripts in the loaded game-script plugin remain
  discoverable by `NpcScriptMetaDataFactory`.
- Partially loadable plugin assemblies retain usable types and report the
  loader exception through logging.
- Existing focused script and GameWorld tests continue to pass.

## Non-goals

- Do not redesign NPC composition or introduce another activation mechanism.
- Do not change the reconnect implementation or AppHost topology.
- Do not alter character, item, widget, area, or game-object script
  registration.
