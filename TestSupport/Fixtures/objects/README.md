# Transform-only canoe station

Read-only revision-742 payloads from `frankvdb7/Hagalaz.GameClient` commit `2d6d99555c87d23d5909ff0f9dfea1e58fa5bdda`, the same pinned `build/ui-test-cache` as the map/model fixtures.

| Payload | Cache address | SHA256 |
| --- | --- | --- |
| object-12163.dat | index 16, archive 47, member 131 | `0501b965f25905b9929a03967ff368396551bac7002eff0af93b862d82e14591` |
| object-12144.dat | index 16, archive 47, member 112 | `1f6588ca7761bfc1ea0d93b91ec551225c85ad27fa978fbf9c585ff76ae00b61` |
| ../models/model-11990.dat | index 7, file 11990 | `0866389a567c3a99869e5e9efbfa481665be4441bb61342f1baaebea3c9c9230` |

Object 12163 has no base shapes/models. Its varbit 1839 transform at index zero selects canoe-station definition 12144, which supplies shape 10/model 11990. The API regressions compare its four single/batch render rotations with that selected definition while preserving source ID 12163. This is a static zero-state preview, not an assertion of the player's live canoe state. Synthetic branch tests cover absent selectors, explicit hidden targets, and the distinction between a short hidden sentinel and a real four-byte object ID 32767.
