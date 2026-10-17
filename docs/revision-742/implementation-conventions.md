# Implementation conventions for revision 742 features

## Authority and version constraints

1. Use a matching revision 742 client/cache capture for opcodes, widgets, CS2 scripts, varp/varbit values, object/NPC/item IDs, interactions, animations and spatial geometry. Don't infer those values from a modern wiki or from a nearby client revision.
2. Record evidence as **verified (742 fixture)**, **historical (dated pre-boundary reference)**, **repository observation**, or **unverified assumption**. Provide a screenshot/packet trace, cache definition, source location or repeatable test to upgrade a claim.
3. Treat the major combat redesign on 20 November 2012 and later skill/quest releases as **out of scope**. Do not copy the later combat level formula, equipment rules or ability UI into this target.
4. Use period sources as feature-existence leads, not numeric truth. Check contradictory sources by testing the actual client/cache.

## Follow existing Hagalaz ownership

- **Network:** `Hagalaz.Services.GameWorld/Network/Protocol/742/` holds the protocol decoder/encoder for the target revision. The message/handler or widget script identifies the gameplay action; never derive gameplay authorization from client-supplied UI state alone.
- **World and entities:** `Hagalaz.Services.GameWorld/Services/MapRegionService.cs`, NPC, ground-item and game-object services own in-world entities. Reuse existing pathfinding, reachability and task scheduling for actions.
- **Character/skill state:** extend the appropriate existing character subsystem or skill script first. Use `Hagalaz.Game.Scripts/Skills/` for skill interactions and `Hagalaz.Services.GameWorld/Services/` for existing data-backed skill services. Existing quests/minigames have their own script families.
- **Items and money:** `Hagalaz.Game.Abstractions/Collections/ItemContainerTransaction.cs` coordinates cross-container mutation; `Mutations` is the existing checked boundary. Do not add a second lock/transaction/rollback API or mutate storage directly from a feature script.
- **Persistence:** `Hagalaz.Services.GameWorld/Logic/Hydrators/` and `Logic/Dehydrators/` participate in character state restoration/snapshots. See `docs/character-persistence.md`; never assume state in a temporary offer, world object or task will outlive logout.
- **Data definitions:** use the established repositories, EF entities and cache codecs for content tables. Do not hardcode large revision-ID lists in skill scripts if data is already supported.

## Minimum feature implementation plan

1. State acceptance criteria, non-goals, status and evidence for the requested subset; create a focused OpenSpec change for significant code work as required by `AGENTS.md`.
2. Trace actual call graph: decoded 742 action -> handler/widget/object/item/NPC script -> service/character state -> item or world mutation -> persistence -> outgoing client update.
3. Identify the **single** owner for each mutable field, and which operation commits the change. Draw state transitions only if there are enough real transitions to need them.
4. Implement one vertical slice with fixtures and tests before multiplying definitions, handlers or mappings.
5. Verify rejected actions and life-cycle boundaries: full containers, changed recipient capacity, stale item instance, concurrent requests, disconnect, death and restart. Keep rollback semantics within the existing transaction ownership.
6. Review for YAGNI/KISS/SOLID as described in `AGENTS.md`: no generic framework for a one-off feature, no duplicate validation, no deferred queues just to hide ownership mistakes.
7. Update the feature guide's status and source notes after implementation. Add fixture files to a documented location only when real evidence exists.

## Test and review commands

- Run focused tests for the touched script/service and the relevant item/world/character regression suites. Examples: `dotnet test Hagalaz.Game.Scripts.Tests/Hagalaz.Game.Scripts.Tests.csproj`, `dotnet test Hagalaz.Services.GameWorld.Tests/Hagalaz.Services.GameWorld.Tests.csproj`, `dotnet test Hagalaz.Game.Abstractions.Tests/Hagalaz.Game.Abstractions.Tests.csproj`.
- For substantive implementations, run `dotnet build Hagalaz.sln`, `openspec validate --all --strict` and `git diff --check`, then check revision-742 protocol/UI parity against a matching client.
- Documentation-only PRs need checked local links, valid code paths, text/forbidden-title scan and a review for unsupported historical claims. They do not establish passing runtime tests.

## Trade and reward conservation invariant

Before a two-party exchange or minigame stake settles, **both** resulting recipients must fit all incoming noncoins and pouch/coin fallback paths. Validate and apply under the **same transaction/ownership boundary**; a preliminary UI free-slot count is only a hint. Cancel and disconnect recovery must preserve escrow in durable state or refuse teardown until it is safe. Do not assume ordinary trade capacity checks eliminate all logout or full-bank failure modes.

Reference implementation starting points: `Hagalaz.Game.Scripts/Characters/TradeExchange.cs`, `Hagalaz.Game.Scripts/Characters/TradingCharacterScript.cs` and `Hagalaz.Services.GameWorld/Services/CharacterLogoutService.cs`.

## Boundaries still requiring evidence

- Numeric recipes, chance formulas, exact tick durations and game-coordinate lists must be verified against 742 material; no such full dataset was read during this audit.
- Feature directory presence is not a completeness metric. A UI with an `OnOpen` method can still be a stub.
- Missing dedicated files in the audited tree do not prove a feature has no generic or data-driven support elsewhere; investigate the full invocation graph before creating new infrastructure.
