## Decision

`BarrowsScript` stays the single owner of per-player run state, persisted only through `BarrowsDto` in the character profile.

- `CharacterScriptBase` invokes `Initialize()` from its constructor, before derived fields exist. `Initialize()` is empty and `LoadRun()` runs at the end of the derived constructor.
- The brother count is computed from the killed flags; there is no second counter.
- Reset happens when the chest is searched (kills cleared, new tunnel). The claim flag is only the guard against a second claim; a fresh run is written when the script is removed after a claim.
- `OnDeath` despawns living brothers only; the run itself is untouched.
- Encodings come from decoded 742 data recorded in `docs/revision-742/reference/minigames.md`.

## Evidence and open choices

- Confirmed from the pinned 742 cache: config 452/453 layouts, rope and door placements, quest id 194 and varbit 9829.
- Period sources: progress saved on leaving without the reward (Darkan archive); random tunnel brother (2011 knowledge base); reset after a claim on leaving (2009 forum thread). Reset at chest search and preserve-on-death come from guides cited in issue #315 and were not independently verified.
- Checked on a live 742 client by the project owner and reported correct: all four puzzle answers, locked doors and the operable door, the wrong-answer effect (door and rope move), and the 30-tick prayer drain.
- The exit message text is original server content; no period source confirms or denies it.
- "You found nothing." for locked Akrisae is a placeholder; Darkan describes a separate quest-only crypt.

## Rejected alternatives

- A Barrows-only quest flag in the profile: duplicates quest state, so Akrisae stays locked instead.
- A second persistence table or kill counter: the profile object is the single owner.
- Loot or puzzle frameworks: out of scope; no 742 data exists for the puzzle protocol or the chest rows.
