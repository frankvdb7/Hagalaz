## Why

World sign-in reaches GameWorld character construction, but the default
character scripts are created while the `Character` constructor is still
resolving its dependencies. `ShopCharacterScript.Initialize` then reads the
character context before `Character` has assigned it, so sign-in fails with a
`NullReferenceException`.

## What changes

- Defer default character-script resolution until `Character` has assigned its
  scoped context.
- Preserve the existing default-script provider and character-scope ownership.
- Add a regression test that fails if resolving the provider constructs a
  context-dependent default script too early.

## Impact

This affects default character-script activation during GameWorld world entry.
It changes no script behavior after activation, NPC activation, network
protocol, persistence, or reconnect behavior.

## Acceptance criteria

- Resolving the default character-script provider does not instantiate default
  character scripts.
- Calling `GetAllScripts` after character context assignment creates the scripts
  in the existing scope.
- GameWorld world sign-in can construct the character without the context-order
  `NullReferenceException`.
- Focused tests, the affected build, strict OpenSpec validation, and the real
  client world-entry check pass.

## Non-goals

- Do not redesign character context lifetime or introduce another script
  activation mechanism.
- Do not change NPC construction, AppHost topology, or the reconnect protocol.
