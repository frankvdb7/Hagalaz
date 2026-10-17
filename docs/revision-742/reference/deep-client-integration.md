# P1 #10 — Revision 742 interface, CS2, varbits and packet integration

**Status: source mapped, runtime parity not verified.** Source snapshots: [Hagalaz \`9c9f862\`](https://github.com/frankvdb7/Hagalaz/commit/9c9f8622305d18ca05fa7099848a0cdb01cf09b6), [companion client \`89b7778\`](https://github.com/frankvdb7/Hagalaz.GameClient/commit/89b7778f6d7ba66a8e5c8d1b18d4b282d3cbfecf). The client already has authoritative in-repository deobfuscation guides, so this document is a **cross-repository implementation handoff**, not a competing packet/UI/CS2 specification. Consult both repositories' \`AGENTS.md\`.

## Primary evidence and existing ownership

| Layer | Client authority | Server authority and application |
|---|---|---|
| Interface cache, component ID, callbacks, layout | [Interface guide](https://github.com/frankvdb7/Hagalaz.GameClient/tree/89b7778f6d7ba66a8e5c8d1b18d4b282d3cbfecf/docs/interfaces); \`GameInterface\`, \`IComponentDefinition\` | \`IWidget\`, \`WidgetScriptMetaData\`, widget scripts, \`Hagalaz.Services.GameWorld/Model/Creatures/Characters/Widgets.cs\` where applicable |
| CS2 code and VM | [CS2 guide](https://github.com/frankvdb7/Hagalaz.GameClient/tree/89b7778f6d7ba66a8e5c8d1b18d4b282d3cbfecf/docs/cs2); \`ClientScript\`, \`ClientScriptsExecutor\`, index 12 | \`Hagalaz.Cache/Types/Providers/Cs2DefinitionProvider.cs\`, \`RunCS2ScriptMessageEncoder\`, client configuration scripts |
| Varps/varbits | \`VarpBitDefinitionRepository\`, index 22 definitions and CS2 varp read/write | \`SetConfigMessageEncoder\`, \`SetVarpBitMessageEncoder\`, \`SetCS2IntMessageEncoder\`, character \`Configurations\` |
| Outbound game packets (client → server) | [108-row outgoing registry](https://github.com/frankvdb7/Hagalaz.GameClient/blob/89b7778f6d7ba66a8e5c8d1b18d4b282d3cbfecf/docs/protocol/outgoing/registry-audit.md), \`OutgoingPacketData\`, \`Class447\`, \`Session.queuePacket\` | \`Hagalaz.Services.GameWorld/Network/Protocol/742/Decoders/\` plus registration; message dispatcher/handlers |
| Inbound game packets (server → client) | [164-row incoming catalogue](https://github.com/frankvdb7/Hagalaz.GameClient/blob/89b7778f6d7ba66a8e5c8d1b18d4b282d3cbfecf/docs/protocol/packet-catalogue.md), \`IncomingPacket\` and consumers | \`Hagalaz.Services.GameWorld/Network/Protocol/742/Encoders/\` and message producers |

### Machine-readable artifacts

- [**272 client game-channel opcode ledger rows**](../data/catalogs/protocol-client-ledger-742.csv): each carries direction, opcode, published framing, source symbol, semantic label and canonical document URL. All \`matched_server_source=unreconciled\` and \`parity=not_runtime_tested\`, deliberately: the client ledger's E3 categories may already have partial server confirmation, but the *new CSV has not independently joined or retested every pairing*.
- [**142 server source files**](../data/catalogs/protocol-server-source-inventory.csv): **38 decoder**, **59 encoder**, **45 \`.rsi.cs\` widget script** files. This counts source paths, **not** mapped opcodes, loaded handlers, or cache components. In particular, multiple option-click decoder *classes* can be declared in a single .cs file. Additional scripts outside \`Widgets/\` (dialogues/minigames) exist and are not included in that widget-file inventory.
- [**Acceptance and gap matrix**](client-integration-verification-matrix.md): actionable, ordered tests and a definition of done. No runtime C# or Java code was changed.

## 1 — Packed widget identity and click ownership

The client interface definition comes from JS5 **index 3**: group=interface ID, file=component child ID; a runtime component key is \`(interfaceId << 16) | componentId\`. The authoritative sources are [interface binary format](https://github.com/frankvdb7/Hagalaz.GameClient/blob/89b7778f6d7ba66a8e5c8d1b18d4b282d3cbfecf/docs/interfaces/binary-format.md) and [menu/input dispatch](https://github.com/frankvdb7/Hagalaz.GameClient/blob/89b7778f6d7ba66a8e5c8d1b18d4b282d3cbfecf/docs/interfaces/context-menu-and-input.md).

Client source path: component menu → context menu selection → \`ServerConnection.onIComponentContextMenuClicked\` → component callback → permission check → option-number-specific \`OutgoingPacketData.iComponent*\` → \`Class447.addInterfaceComponentClickData\` → \`Session.queuePacket\`. The documented client behavior **runs its callback before server-option permission selection**; tests must preserve this ordering. On the server, \`InterfaceComponentClickMessageDecoderBase.TryDecodeMessage\` reads packed ID via **middle-endian int**, data 1 as **little-endian+A short**, data 2 as **little-endian short**. It maps high 16 bits to interface ID and low 16 to child ID. Concrete option classes translate to \`ComponentClickType\`. Dispatch then goes through widget instance/script handlers and event permissions, which must be traced independently of successful bytes decoding.

A currently inspected example is \`Hagalaz.Game.Scripts/Widgets/Tabs/CombatTab.rsi.cs\`: it calls \`InterfaceInstance.SetOptions\` for children 7–10, registers an \`OnComponentClick\` left-click handler, updates profile combat style through mediator action and clears selected autocast. Its script source is **not** proof of the interface group ID or an active runtime widget mapping without observing the opener / provider. Record both interface ID and child ID in any test trace; avoid conflating child ID with a globally unique widget ID.

Other component paths: drag/drop, number/color/text input, close/remove, use-on-entity, use-on-object and use-on-component. They have distinct message types and must not be inferred from ordinary left clicks. Treat option 1 separately from the generic notion of left mouse button (the client menu option and physical pointer button are not the same contract).

## 2 — CS2 scripts and call contract

Client cached scripts live in JS5 **index 12**. Existing client documentation owns the script codec, VM, opcodes, trigger/callback encoding, stack signatures, roundtrip compiler and source-name uncertainty. Relevant anchors: \`ClientScript\`, \`ClientScriptsExecutor\`, \`CS2\`, \`Cs2IntRepository\` and the client [CS2 documentation authority](https://github.com/frankvdb7/Hagalaz.GameClient/blob/89b7778f6d7ba66a8e5c8d1b18d4b282d3cbfecf/docs/cs2/documentation-authority.md). Do not infer semantic script purpose from numeric ID.

Server \`RunCS2ScriptMessageEncoder\` writes **opcode 29**, variable-short body. It serializes a type string (\`s\` for string parameter, \`a\` otherwise), null terminator, **reversed** parameter values (string vs big-endian int), then a **big-endian script ID**. The client must parse and execute the same signature and argument order; null/unexpected argument types and missing script IDs are characterization cases. The existence of \`Hagalaz.Cache/Types/Providers/Cs2DefinitionProvider.cs\` is not proof a given script is present in approved cache bytes, nor that its caller passes the correct arguments.

A full bridge row needs \`script_id\`, \`invoke_source\`, \`arg_signature\`, \`expected_on_open_or_click\`, \`client_VM_effect\`, \`component_ids\`, \`varp_ids/varbit_ids\` and a saved read-only trace. Existing client [script index](https://github.com/frankvdb7/Hagalaz.GameClient/blob/89b7778f6d7ba66a8e5c8d1b18d4b282d3cbfecf/docs/cs2/scripts/index.md) holds researched examples but not a verified full all-script bridge.

## 3 — Varp, varbit and client-int state

\`SetConfigMessageEncoder\` chooses **opcode 10** for values fitting signed byte (**-128..127**), writing a little-endian config ID followed by one byte, and **opcode 82** for other values, writing mixed-endian int value and big-endian+A short ID. Verify these exact transforms against client incoming catalogue and live config state; the same logical config can cross byte/int size paths. \`SetVarpBitMessageEncoder\` and \`SetCS2IntMessageEncoder\` are separate messages; don't mislabel a varbit as a full varp or as a CS2 int. Varbit identity must resolve base varp + least/most bit indices using the *actual client index-22 definition*, not guessed bit masks.

For a profile/config mutation, trace \`ProfileConstants\` → character \`Configurations\` → encoder opcode/field transforms → client \`VarpBitDefinitionRepository\`/config mutation → optional CS2 dependent callback → rendered UI. Verify initialization/on-open, fast sequences, sign/size boundaries, logout/reconnect and switching UI tabs. Never add another in-memory config store to compensate for a missing trace; identify existing authoritative state.

## 4 — Protocol registration and migration

The [client 108 outgoing](https://github.com/frankvdb7/Hagalaz.GameClient/blob/89b7778f6d7ba66a8e5c8d1b18d4b282d3cbfecf/docs/protocol/outgoing/registry-audit.md) and [164 incoming](https://github.com/frankvdb7/Hagalaz.GameClient/blob/89b7778f6d7ba66a8e5c8d1b18d4b282d3cbfecf/docs/protocol/packet-catalogue.md) registries cover the game channel, **not** handshake/authentication, lobby, auxiliary channels or every dynamic factory. The Hagalaz directory has 38 decoder source files and 59 encoder source files; these file counts are neither opcode counts nor evidence of complete coverage. Do not conclude that \`108 - 38\` packets are unimplemented.

A parity row can graduate from \`unreconciled\` only after proving: (1) client opcode/framing and exact payload; (2) server's active opcode registration, not only decoder class existence; (3) the same wire transforms and signedness; (4) message dispatch, authoritative validation and side effect; (5) symmetric state after server acknowledgement or outbound update. Disagreements with existing client evidence should be recorded in the owning client protocol document; this dossier cross-links rather than copies 272 hand-written packet descriptions.

### Sample exact bridge: component option 1

Companion outgoing registry lists client opcode **4**, fixed body **8**, identity \`interfaceComponentOption1\`. Server \`InterfaceComponentLeftClickDecoder\` subclasses the shared \`InterfaceComponentClickMessageDecoderBase\`, which decodes 4-byte packed component ID and two 2-byte action data values as specified above. This is a **payload-semantic alignment**, not yet an independently verified active opcode-4 registration. Next verify the server's concrete decoder opcode registration, selected widget instance and scripted dispatch.

### Sample outbound bridges

\`RunCS2ScriptMessageEncoder\`: **29**, variable-short. \`SetConfigMessageEncoder\`: **10** (small signed-byte range) / **82** (large int). Other UI/config emitters include \`SetVarpBitMessageEncoder\`, \`SetComponentOptionsMessageEncoder\`, \`SetComponentVisibilityMessageEncoder\`, \`DrawInterfaceComponentMessageEncoder\` and \`RemoveInterfaceComponentMessageEncoder\`. Do not assume an encoder's source method is reachable without checking DI/registration and a production caller.

## 5 — Priority risk and missing evidence

1. Static source inventories are not bound to active DI registrations; **verify actual packet registrations**, especially option variants and distinct input/use-on packets.
2. The source client callback-before-permission ordering can cause local CS2 effects for actions rejected as network packets; confirm whether the UI intentionally allows this and whether the server independently validates action.
3. Session/lobby/game channel framing must never be conflated. Keep authentication decode under its existing handshake owner.
4. \`RunCS2Script\` parameter signature/order, unusual dynamic input and variable-size packet frames require deterministic roundtrip tests.
5. Config ID/bitfield overlap, byte/int threshold, signed transforms and on-open event ordering require client/server paired assertions.
6. Widget script source and component IDs cannot be projected into actual index-3 cache presence without binary fixture.
7. Some legacy decoders may still live outside \`Hagalaz.Services.GameWorld/Network/Protocol/742\` or in earlier script modules. Audit production registrations, **not just naming conventions**, before opening migration issues or removing code.
8. Per-opcode client E2/E3 status belongs to the authoritative client ledger. The exported Hagalaz snapshot intentionally leaves the independently proved server link unset.

**Completion gate:** use [the explicit verification matrix](client-integration-verification-matrix.md). A complete 742 parity claim requires pinned cache verification, production registration audit, exact client/server decode vectors and at least one live trace for every feature family. This pass performed none of those runtime checks. No speculative refactor, duplicate protocol registry or direct game code edit was made.
