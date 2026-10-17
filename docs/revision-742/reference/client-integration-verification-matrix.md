# P1 #10 — Client/server integration verification matrix

**Source documentation and inventories exist; acceptance tests are not executed.** See [deep bridge dossier](deep-client-integration.md), [client opcode ledger](../data/catalogs/protocol-client-ledger-742.csv) and [server source inventory](../data/catalogs/protocol-server-source-inventory.csv).

| ID | Concern | Characterization / regression test | Required evidence |
|---|---|---|---|
| I01 | UI cache | Index-3 interface group + child file → same packed ID on both sides | verified cache fixture hash and component record |
| I02 | UI parent | Component parent links and dynamic arrays preserve on-open hierarchy | deterministic client fixture |
| I03 | UI menu | Option 1..10 routes to distinct client opcode and matching server click type | raw bytes and registered decoder |
| I04 | UI callback | Client CS2 callback happens before permission check without server authorization bypass | source trace and rejected-click test |
| I05 | UI option | Server widget handler and \`SetOptions\` align with client menu permission | opened widget ID, child range and click result |
| I06 | UI dragging | Drag/use-on-component, entity, object, NPC, ground item are independently parsed | field-by-field binary vectors |
| I07 | UI input | Number, color, string/text and URL actions decode without truncation | boundary/sentinel payload fixture |
| I08 | UI lifecycle | Closing widget detaches handlers and old component IDs cease to work | reopen/logout/reconnect |
| I09 | CS2 | Opcode-29 argument signature, reverse argument values and script ID | roundtrip binary fixture |
| I10 | CS2 | String/int mixed parameters, missing scripts and null data | explicit error/fallback contracts |
| I11 | CS2 | Client VM callback trigger reads current varp/varbit after config update | pinned script+varbit fixture |
| I12 | CS2 | Dynamic interface CS2 scripts re-run on widget reopen at expected ordering | client trace |
| I13 | Config | SetConfig signed byte -128/-127/0/127/128 and int path | opcodes 10/82 raw fixture |
| I14 | Config | Integer endian+A/mixed-endian transforms match client reader | paired encoder/decoder tests |
| I15 | Config | Varbit masks preserve adjacent bits in base varp | pinned index-22 definitions |
| I16 | Config | Separate SetVarpBit and CS2 int paths are not conflated | paired packet trace |
| I17 | Protocol | All 108 client outgoing rows joined to actual registered server decoder or explicit gap | registration manifest keyed by opcode |
| I18 | Protocol | All 164 server incoming rows joined to actual active server encoder or explicit gap | registration manifest keyed by opcode |
| I19 | Protocol | Fixed vs var-byte/var-short body boundaries and ISAAC state | deterministic per-direction vectors |
| I20 | Protocol | Login, lobby and world game channels and reconnect frames remain separate | stateful session traces |
| I21 | Protocol | Zero-length/unrecognized opcode/malformed short packet fails without partial mutation | hostile packet vector |
| I22 | Protocol | Alternate component option callbacks do not route to generic left click incorrectly | option mapping and handler assertion |
| I23 | Game state | Combat style and spell autocast UI → profile → server validation → config UI | end-to-end game capture |
| I24 | Game state | Bank inventory slot / drag / component actions preserve item identity and permissions | mutation and UI capture |
| I25 | Game state | NPC/object contextual menu and use-on actions preserve entity and region identity | routed action capture |
| I26 | Game state | Prayer book and quick prayers produce correct config/varp/CS2 state | activation/deactivation trace |
| I27 | Game state | Dialogue state, options and close event cannot execute stale callbacks | opened/closed instance tests |
| I28 | Client parity | Widget/CS2/varbit index presence and exact revision hash is recorded | approved fixture |
| I29 | Production topology | No duplicate active packet owner; scripts assembly legacy handlers identified | DI/service registration review |
| I30 | CI | Protocol roundtrip fixture and widget/config contract regressions pass | test report/build and strict spec validation |

## Evidence row contract

For every successful mapping persist \`client_commit,server_commit,channel,direction,opcode,framing,client_symbol,client_producer_or_consumer,server_registered_encoder_or_decoder,server_dispatch,bytes_fixture_sha,cache_fixture_sha,decision,expected_effect,actual_effect,test_ref\`. Use independent entries for packet variants. For widget mappings also persist \`interface_group,component_file,packed_id,script_registration,CS2_on_action,varps,varbits\`. Where evidence is missing, leave the mapping **unreconciled** rather than guessing based on names.

First execution order: I03/I17/I18 (active transport ownership); I09/I13/I14 (byte contracts); I01/I04/I05/I22 (widget dispatch); I15/I16/I26 (bitfields); I06/I07/I08/I10–I12 (dynamic UI/CS2); I19–I21/I29 (session robustness); I23–I25/I27/I28/I30 (end-to-end integration).

**Documentation-pass acceptance:** source-anchored maps for client and server inventories, real examples with exact byte contracts, 30 reproducible cases, existing client documentation authority honored and explicit missing evidence. **Full implementation-parity acceptance:** all 272 game-channel opcodes classified against active server registrations (including genuine gaps), widgets/CS2/varbit IDs verified from binary, tests and client captures passing, no silent fallback or authorization bypass. The latter is not achieved here.
