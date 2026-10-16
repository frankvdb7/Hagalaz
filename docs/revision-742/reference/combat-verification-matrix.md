# P1 #9 — Combat and character verification matrix

**Status: partially source-audited.** Date: 16 October 2026. See [detailed combat dossier](deep-combat-character-mechanics.md). No external archive contents, live DB, client packets or runtime tests were accessible for this pass. All tests below are **specified but not run**.

## Test-first matrix, keyed to current owner

| ID | Domain | Starting state/input | Invariant / expected result | Source owner |
|---|---|---|---|---|
| C01 | Accuracy | melee attacker vs equal/greater/lower defence rolls | controlled random hit outcomes and distinct miss \`-1\`; no divide-by-zero | \`CharacterCombat.GetMeleeDamage\` |
| C02 | Accuracy | rapid vs accurate ranged; high defence | style bonuses and both attack/defence rolls applied exactly once | \`CharacterCombat.GetRangedDamage\` |
| C03 | Accuracy | magic, defence and Magic-level permutation | 70/30 magic/defence weighting and testable no-hit outcome | \`CharacterCombat.GetMagicDamage\` |
| C04 | Damage | max strength bonus with worn set, prayer, skill drain | source max-hit formula, rounding and set bonus; bounded real damage | \`CharacterCombat.GetMeleeMaxHit\` |
| C05 | Damage | enchanted bolts, special, Void and ranged styles | one selected modifier, correct damage cap and ammo consumption | \`CharacterCombat.GetRangedMaxHit\`, ranged scripts |
| C06 | Timing | unarmed / item metadata / ranged rapid / miasmic | speed 4 fallback, source item speed, rapid -1 and slow x2 | \`EquipmentStore\`, \`CharacterCombat.GetAttackSpeed\` |
| C07 | Timing | spell selected and autocasting concurrently | selected spell precedence and spell casting speed, no unwanted weapon slow | \`CharacterCombat.GetCastedSpell\` |
| C08 | Delayed damage | target dies / vanishes / changes dimension before hit | handle re-resolution blocks invalid hit, XP, reflection and loot | \`CreatureCombat.PerformAttack\` |
| C09 | Special attack | energy cost-1, cost and cost+1 | exactly one debit on a successful supported special | \`CharacterCombat.AttackTick\` |
| C10 | Special attack | unsupported handler throws | no cost consumed for an unperformed special; no unintended unarmed hit | \`CharacterCombat.AttackTick\`, \`EquipmentScript\` |
| C11 | Multi-hit | dagger/claws/dark bow vs protection, target disappearance | exact hit count, independent protection and once-only XP/kill credit | weapon scripts, \`CreatureCombat\` |
| C12 | Protection | protect ranged active, incoming melee/magic/dragonfire | prayer must not reduce an unrelated damage type | \`CharacterCombat.IncomingAttack\` |
| C13 | Protection | protect magic active, incoming ranged/melee | no wrong-type absorption; standard/full variants distinguished | \`CharacterCombat.IncomingAttack\` |
| C14 | Reflection | melee/ranged/magic/summoning deflect with reflected hit | no self-recursion; correct graphic/owner/damage and delay | \`CharacterCombat.IncomingAttack\` |
| C15 | Prayers | activate below threshold / wrong book / zero prayer | no activation and no charge; event guard enforced | \`Prayers.ActivatePrayer\` |
| C16 | Prayers | switch mutually exclusive normal/curses | incompatible bonuses removed, status/config bits consistent | \`Prayers\`, \`PrayerConstants\` |
| C17 | Prayers | multiple drains with +0 and +30 Prayer bonus | elapsed-tick drain scale and zero-point deactivation | \`Prayers.Tick\` |
| C18 | Magic | standard spell with boosted Magic and gear bonus | maximum hit modified once; advertised max matches sampled bound | \`StandardCombatSpell\`, \`CharacterCombat.GetMagicDamage\` |
| C19 | Magic | missing one rune, stunned, successful cast, interrupted cast | exactly-once rune debit and correct XP timing | \`CharacterCombat.AttackTick\`, spell subclasses |
| C20 | Magic | AoE curse/magic target plus other non-PvP-eligible players | each target independently authorized and hit limits enforced | ancient magic scripts, area scripts |
| C21 | NPC | default aggressive vs neutral, level mismatch, single combat | target chosen only when eligible and in range | \`NpcScriptBase.AggressivenessTick\` |
| C22 | NPC | simple pathfinder, separating wall and projectile obstacle | no attack/hit through forbidden collision or blocked LOS | \`NpcCombat\`, movement/pathfinding |
| C23 | NPC | boss override, reactive damage/poison, death | subclass methods win; loot once; respawn/unregister once | \`NpcCombat\`, registered NPC scripts |
| C24 | Familiar | summon twice / wrong pouch / insufficient points | no duplicate familiar, no pouch loss on failed spawn | \`SummoningSkillService\`, \`FamiliarScriptBase\` |
| C25 | Familiar | special scroll absent, present, depleted points | exactly-once scroll removal/point debit; no negative state | \`FamiliarScriptBase\` |
| C26 | Familiar | logout, death, dismissal and restore with carried items | no orphan familiar, inventory duplication or stale script handlers | \`FamiliarCharacterScript\`, hydrators |
| C27 | Wilderness | PvP level delta below, equal, above zone threshold | symmetrical permission and matching UI level range | \`Wilderness.CanAttack/CanBeAttacked\` |
| C28 | Wilderness | familiar attack / familiar defending player attack | master's effective combat level applied, no loophole | \`Wilderness\`, \`FamiliarScriptBase\` |
| C29 | Teleports | Wilderness levels 20/21 and item vs standard teleport | correct gating, no disallowed teleport when client still offers it | \`Wilderness.CanDoStandardTeleport/CanDoItemTeleport\` |
| C30 | Death | standard death, skull, protection prayer, unavailable killer | conserved total inventory/gear, correct keeper/dropped split | \`CharacterCombat.OnDeath/OnKilledBy\` |
| C31 | Death | delayed hit after logout/death/region unload | no re-attribution or duplicated owned ground drops | \`CreatureCombat\`, \`CharacterCombat\` |
| C32 | Duel | disable magic/ranged/melee, food, prayers and special | every rule blocks server operations, not only UI actions | \`DuelRules.ApplyRules\` |
| C33 | Duel | no summoning, existing familiar, foreign player attack | no bypass via pet or alternate target, no skull/drop | \`DuelArenaCombatScript\` |
| C34 | Duel | fatal hit/trade escrow and participant logout | victory/draw/cleanup and item ownership exactly once | \`DuelArenaCombatScript\`, stake exchange |

## Evidence and report format

Each test record must carry: \`test_id,source_commit,fixture_sha,db_snapshot,setup,actual,expected,result,client_capture_ref\`. For probabilistic cases provide a deterministic RNG or captured fixed seed and check boundary outputs rather than only averages. For derived IDs and costs, preserve [special source registrations](../data/catalogs/combat-special-energy-script-registrations.csv) and [prayer source constants](../data/catalogs/combat-prayer-source-constants.csv) separately from any version-742 cache export.

Test order: (1) C12–C14 protection; (2) C09–C10 special debit; (3) C18 magic scaling; (4) C01–C08 deterministic combat vectors; (5) C15–C17 prayers; (6) C19–C26 NPC/magic/familiar; (7) C27–C34 PvP/death/conservation. Follow repository \`AGENTS.md\`; changes to production should go into a properly scoped OpenSpec change and should reuse existing combat owners, not introduce a second calculation pipeline.

## Acceptance / non-goals

Acceptance for **documentation pass**: correct source map, explicit version/evidence caveats, 50 prayer constants, 49 source weapon IDs with costs, source-backed action list and reproducible test cases for each requested domain, linked in the P1 handoff. These can be validated structurally without pretending tests ran.

Acceptance for **full P1 completion**: physically verified index-18/19 definitions, current relevant DB rows, all 34 tests passing or explicitly waived with evidence, instrumented client/server action captures, no unresolved correctness bugs or unknown effective owner for requested mechanics.

Non-goals of this PR: runtime combat formula rewrite; "canonical" research values substituted for actual server inputs; creating another combat coordinator, locking protocol, request pipeline or generalized integration harness; undocumented PvP policy changes.

## Extraction audit cross-check

The [ancient spell rule table](../data/catalogs/combat-ancient-spell-script-rules.csv) contains five families of four variants, parsed from source arrays without inventing missing config IDs. The [prayer constant table](../data/catalogs/combat-prayer-source-constants.csv) contains 30 standard and 20 curse indexed rows; [special energy table](../data/catalogs/combat-special-energy-script-registrations.csv) contains 49 unique item IDs from 19 concrete equipment scripts. These are fixture-independent source facts, and the tests above remain unexecuted.
