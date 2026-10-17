# Progression: levels, XP, boosts and effect ownership

## Known target-era baselines

Most skills cap their displayed base level at 99; Dungeoneering at 120. Constitution starts at level 10 (100 life points). The ordinary cumulative XP formula is:

```text
points(level) = Σ floor(i + 300 * 2^(i/7)), for i = 1..level-1
xp(level) = floor(points(level) / 4)
```

The computed [1–120 table](../data/experience-levels-1-120.csv) yields 83 at level 2, 13,034,431 at 99 and 104,273,167 at 120. It should be cross-checked against the current Hagalaz implementation and client 742 skill identifiers before any replacement.

## Current Hagalaz state

`SkillsCharacterScript` and character statistics, the skills tab, level-up events and statistics hydration/dehydration own skill state. Production task scripts should call the authoritative `Statistics.AddExperience`, not set displayed levels or create another XP ledger.

Stats have **base XP level**, **temporary effective/drained level**, and possibly area/equipment/auras/quest restrictions. A stat boost can change an action's eligibility without changing the XP-derived base level. Constitution uses a scaled life-point display, which must not be confused with actual XP or combat damage unit.

## Completion requirements

- Map each skill ID and display order to index-3 UI/CS2 and outgoing skill packet; reveal 742-era skill set, membership and cap.
- Capture exact XP awards for raw food, quest reward, combat dealt damage, skill success, bonus outfits/urns and resource multi-harvest; distinguish per-attempt XP and per-success XP.
- Combat training XP must be awarded from actual damage and selected combat style; one hit cannot trigger multiple unrelated style grants.
- Temporary stat restoration interval/boost stack order must be characterized. Don't implement a generic buff engine if existing states and character statistics handle the actual cases.
- Character snapshot and periodic updates carry XP and boosted/current stats in compatible units. Test relog, death, level-up cap and negative/double reward.

## Minimal invariants

For any action, `baseLevel = LevelForExperience(XP)`, and a change to temporary level must not mutate XP. XP can be fractional internally; rounding must be at the same layer across rewards and persistence. Cap must apply to display/eligibility independently from total stored experience.
