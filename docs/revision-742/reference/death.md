# death — revision 742 research data

**Evidence tier:** historical secondary references; revision-specific constants require target build verification.

## Historical item retention candidate rules

- In dangerous ordinary deaths, **three** items are kept when unskulled, **four** if Protect Item is active.
- When skulled: ordinarily **zero** items, or **one** with Protect Item.
- Safe deaths (including duel arena) do not use ordinary dangerous-drop behavior.
- A preserved source describes a later 2011 change that allows selecting three kept items **outside Wilderness**, while in Wilderness trade value determines which to retain.
- Certain nontradable/vanishing items have exceptions; the pouch remains protected separately.

Source: [items kept](https://wiki.darkan.org/Items_Kept_on_Death), [duel arena](https://wiki.darkan.org/Duel_Arena), [money pouch](https://wiki.darkan.org/Money_pouch).

**To implement:** capture every death type, risk-preview configuration, grave creation, reclaim item subset, timer/expiry, item breakage and currency behavior from versioned evidence. The 13 November 2012 dungeon XP/gravestone update may have occurred **within** revision 742, so the precise target cache date matters. Do not treat every 742 build as temporally identical.

## Evidence completion gate

The supplied historical links provide candidate behavior, not a full cache/data export. Before implementing, verify the exact target build's IDs, mechanics and production invocations; test consistency across sessions, interrupts and restarts.

[Implementation guide](../death.md) · [Research policy](../research-evidence.md)
