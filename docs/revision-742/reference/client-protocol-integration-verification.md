# Revision 742 — P1 #10 client/server integration verification matrix

**Scope:** interfaces, CS2, varbit/varp/global state, incoming/outgoing protocol, end-to-end gameplay. **Current result: planned, NOT EXECUTED.** For evidence/owners see [the detailed dossier](deep-client-interface-cs2-protocol-integration.md). Baselines: Hagalaz \`9c9f8622305d18ca05fa7099848a0cdb01cf09b6\`; Hagalaz.GameClient \`89b7778f6d7ba66a8e5c8d1b18d4b282d3cbfecf\`.

| Case | Subsystem | Required assertion | Source boundary |
|---|---|---|---|
| P01 | Client outbound registry | Every opcode 0..107 has one framing declaration and exact server length agreement | client outgoing ledger / \`ClientProtocol742._payLoadSizes\` |
| P02 | Server decoder registrations | All \`AddDecoder\` opcode registrations point to the matching client producer and one concrete decoder; missing opcodes explicitly classified | \`Startup.AddRaidoProtocol\` |
| P03 | Opcode encryption | Client opcode ISAAC and server incoming subtraction consume keys only on complete messages; fragmented prefix/body resumes correctly | \`ClientProtocol742.TryParseMessage\` |
| P04 | Client component menu | Options 1..10 map to opcodes 4/22/13/76/59/103/37/69/80/28 | client context-menu dispatcher / server startup |
| P05 | Component byte layout | Packed ID, extra1, extra2 decode correctly for BE/LE/middle/A transforms; signed/sentinel values preserved | \`InterfaceComponentClickMessageDecoderBase\` |
| P06 | Server authorization | Forged extra fields or closed/unowned interfaces cannot perform server state mutation | \`ComponentHub\` / \`Widget\` / concrete script |
| P07 | Widget close/reopen race | Queued action from old widget instance cannot trigger a newly created widget with same numeric ID | \`ComponentHub.OnComponentClick\` |
| P08 | Dynamic children | Cache child ID, runtime child selector and item slot are not conflated; variant inputs resolve correct handler | client \`Class166\` / widget handlers |
| P09 | Use-on and drag | Both source/destination identities and extra pairs are preserved and individually validated | use-on-component and drag decoders / hub |
| P10 | Enclosed zone framing | Non-null grouped zone packet 162 has required variable-short prefix and exact client parser length | \`MapRegionPartUpdateMessageEncoder\` |
| P11 | Nested zone metadata | An embedded zone submessage does not replace the enclosing opcode/size and is payload-only | \`RaidoMessageBinaryWriter\` / nested encoders |
| P12 | Zone submessage map | Indices 1,2,4,6,7,9,12,13,14 map to expected embedded payload and world state | zone encoder \`_messageIndexMap\` / client zone parser |
| P13 | Server -> client framing | Every 59 registered encoder classes emits a packet with valid opcode, framing and exact byte size | server encoder crosswalk / client 164 ledger |
| P14 | Conditional opcodes | Large/small NPC scene (65/72), config small/large and CS2 global small/large select correct branch | encoder conditional source |
| P15 | Character menu option 10 | Client opcode 46 either dispatched to authorized game logic or explicitly unsupported without side effects | client outgoing ledger / server startup |
| P16 | Full UI open | Frame, parent slot, component group and opener script run in correct order without duplicated callbacks | \`WidgetContainer\` / \`Widget\` / client index 3 |
| P17 | UI close | Close/removed message clears server/client handler ownership with no late mutation | \`WidgetContainer\` / \`ComponentHub\` |
| P18 | Context menu permission | Client callback-before-permission ordering cannot bypass server authorization | client UI context menu / server concrete scripts |
| P19 | CS2 call wire | Opcode 29 signature is terminated, types and reverse argument writes match client VM call stack | \`RunCS2ScriptMessageEncoder\` / client VM |
| P20 | CS2 script 150 | Item grid setup actual cached script signature, component and options match each server callsite | index 12 script 150 / server source |
| P21 | CS2 757 and 5561 | Pouch-grid and money-pouch changes invoke exactly intended script side effects | index 12 scripts 757/5561 |
| P22 | CS2 resource catalogue | All 11 observed literal CS2 IDs decode from pinned group=id/file=0 and return captured signatures; dynamic calls separately recorded | index 12 / call inventory |
| P23 | CS2 callback order | \`onOpen\`, click, config, container, skill and global triggers respect decode and event order | client callbacks and change-generation |
| P24 | Standard varp | Opcodes 10/82 round-trip signed-byte boundaries, full integer, transform, config trigger | \`SetConfigMessageEncoder\` |
| P25 | Varbit | Opcodes 156/99 round-trip id/value and parent varp bit mapping for index-22 definitions, including nonoverlap | \`SetVarpBitMessageEncoder\` / cache codec |
| P26 | CS2 int/string global | Int opcodes 22/152 and string 50/55 update distinct vars with precise length and trigger delivery | \`SetCS2Int/SetCS2StringMessageEncoder\` |
| P27 | Inventory/containers | Full 67, delta 94 send correct container IDs, capacity/slots, item+1, count over 254 | \`DrawItemContainer\`, \`SetItemContainer\` |
| P28 | Bank/shop action | Open -> CS2 -> container -> click/use-on -> authorized transaction -> updated container/varp -> close | widget and item ownership |
| P29 | Prayer/combat tab | Prayer enable/disable, style option and special updates match client settings and actual server states | UI/Prayers/Combat interfaces |
| P30 | Quest/dialog/input | Prompt string/number/color submission targets owning open interface and respects cancellation/replay | \`ComponentHub\`, event handlers |
| P31 | Region/lodestone | Teleport/open map/region update cannot mix stale scene coords, dimension or interface ownership | region/zone encoders, widget |
| P32 | Logout/reconnect | Pending widget events, CS2 globals and item-container updates do not replay as stale mutations on next session | hub scheduling, hydration and UI reset |

## Acceptance and test evidence format

For each case record \`case_id\`, server/client commit, approved fixture ZIP SHA-256, decoded index/group/file checksums, DB snapshot if needed, exact setup, raw opcode/frame bytes (scrub secrets), expected/observed fields in wire order, game-state assertion, source handler path, result, any waiver rationale. For the high-priority 162 issue, use a *non-null grouped message with at least one concrete submessage*, then independently decode with the unchanged client parser rather than inspecting only the C# writer's logical fields.

**Documentation-scope acceptance:** all 108 client outbound lengths checked; all 59 server encoder classes considered; all 164 client inbound identities referenced through the canonical ledger; source-evidence tables, known edge conditions and 32 distinct test descriptions; data and README linkage; source statements don't claim binary/runtime proof.

**Full feature completion gate:** approved cache indexes 3/12/22 decoded, actual interface/CS2/varbit joins produced, exact transform/body-length tests including zone 162 pass, all 32 scenarios executed or evidence-waived, relevant gameplay flows observed end-to-end, source mismatches fixed using current owners. **No C# or client runtime code should be changed in this documentation-only PR.**

**Non-goals:** second packet registry in GameClient, mass rename/deobfuscation, speculative opcode interpretations, automatic handler generation for 33 unregistered opcodes, replacing the CS2 VM, introducing a parallel widget event pipeline.
