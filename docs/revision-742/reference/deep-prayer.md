# Prayer and ancient curses — revision 742 detailed implementation research

## Historical data catalog

The companion [20-curse CSV](../data/researched/ancient-curses.csv) lists all period curse names, required levels, *seconds per prayer point* with zero prayer bonus, effect family and source. The [archived curse article](https://wiki.darkan.org/Ancient_Curses) states that completing **The Temple at Senntisten**, reading the Ancient Hymnal and using the Altar of Zaros unlock and switch to curses. The altar can also restore current Prayer and gives an overcharge; the actual boosted cap/effect must be verified against the target build. Switching to ordinary prayers is possible at standard altars. **The 20 curse rows are not widget/prayer ordinal IDs.**

### Curse category semantics

| Group | Events / exclusions to verify |
|---|---|
| Sap | drains opponent attack/ranged/magic/special over time; depends on landing hits and repeated effect rules |
| Leech | temporary opponent debuff and own boost; never permanently alter a base XP level |
| Deflect | protection and reflected damage; does not imply absolute immunity to all boss/pvp hits |
| Soul Split | heals from dealt damage and drains opponent prayer; exclude zero-damage, protected, immune and already-dead targets |
| Wrath | death-triggered damage, with attacker and safe-zone policy |
| Turmoil | stat boost with opponent-scaling modifiers and required combat conditions |
| Protect Item | dangerous-death item retention with current skull state, no direct bank transfer |

No simultaneous activation of conflicting overhead protections. The specific allowed/excluded combinations must be read from existing server `Prayers` code and exact client 742 configurations, not simply assumed from group labels.

## Hagalaz code and storage links

- `Hagalaz.Services.GameWorld/Model/Creatures/Characters/Prayers.cs` owns activation, deactivation, eligibility, drain rate, dynamic combat bonuses, refresh and per-tick processing. Its visible methods include `Tick`, `CheckRequirements`, `GetDrainRate`, `ActivatePrayer`, `DeactivatePrayer`, `DeactivateAllPrayers`.
- `Hagalaz.Game.Abstractions/Model/Creatures/Characters/IQuickPrayer.cs` and `Widgets/Tabs/PrayerTab.rsi.cs` connect saved selected quick-prayers to UI operations.
- `Hagalaz.Services.GameWorld/Services/PrayerService.cs` supplies data-backed prayer definitions; item offering XP is **not the same as active prayer bonuses**, even though `SkillsPrayerDefinition` stores item XP and type.

## Suggested normalized data record (not a new schema mandate)

```text
Prayer: book, ordinal, requiredLevel, unlockQuestState, drainRate,
    offensive/defensive modifiers, groupExclusions, overheadGraphic,
    activationConfig, prayerBonusMultiplier, targetEffect, refreshVersion
```

Trace the existing enum/state/bitfields first. If they already encode the rule, preserve the authority.

## Validation matrix

1. Login with 0/full/boosted prayer: active state and selected quick-prayers match authoritative state; no active curse without unlock.
2. Activate two same-group or conflicting overhead effects: old one deactivates and bonuses are applied exactly once.
3. Drain across tick boundaries with each equipment prayer bonus and multiple active effects: no underflow; zero turns effects off.
4. Reflection versus Soul Split, poison, area damage, boss attacks and player damage: target identity, damage amount and event ordering are correct.
5. Prayer book switching while a prayer is active, in combat, at altar, or via reconnect: no stale buffs or leaked prayer drain timers.
6. Skull/death with Protect Item: items remain conserved and computed loss preview agrees with actual death policy.
7. Source historical formula conflicts (PvP protection, drain/timing, effect caps) remain unresolved until a 742 capture.

## Remaining data necessary for full parity

The other standard prayers/curses book table, exact UI icon ordinals and packed config/varbits, all overhead graphical IDs, drain with bonuses, target-specific multipliers and quest state from the pinned cache must be exported. The CSV alone is a **historical candidate**.

## Full standard prayer and offering datasets

The [standard-book catalog](../data/researched/standard-prayers.csv) now records all **30** candidate ordinary prayers and their required levels, base drain intervals and effect categories. The [bone/ash offering table](../data/researched/prayer-offering-xp.csv) covers **23** base XP values. The latter **does not** include gilded altar, incense, Ectofuntus or quest-specific multipliers; source descriptions provide Ectofuntus at **4x** base, and fully equipped gilded altar with two burners up to **3.5x** base, conditional on live burning state. [Period Prayer reference](https://wiki.darkan.org/Prayer).

The period description says Prayer points were scaled to **ten points per skill level** from September 2011. It also gives a base prayer-bonus interval approximation:

```text
modifiedSecondsPerPrayerPoint = baseSecondsPerPrayerPoint * (1 + prayerBonus / 30)
```

This is a *historical reference formula*, not a confirmed 742 server tick implementation. Verify rounding, tick accumulation, ten-point scaling and zero state in `Prayers.Tick` and `GetDrainRate`. Overcharge from special altars must not become permanent base Prayer. Always keep altar/incense timer ownership separate from ordinary point restoration.

Regression additions: 0 and +15 prayer bonus protection over 30 seconds; simultaneous prayer drain stacking, first/last timer tick, ordinary-to-curse book switching, prayer point maximum versus effective boost, offering item with both lit and extinguishing burners during a quantity-X batch, and Ectofuntus bonus without additional XP duplication.
