# Clans and clan citadels — revision 742

**Repository assessment:** Core clan model present; citadel gameplay not identified. Code entry points do not imply full behavior coverage.

## Historical scope

Clan membership, ranks, permissions, chat and motifs plus period-appropriate clan-owned citadel maintenance and skilling plots.

## Existing Hagalaz entry points

- `Hagalaz.Game.Features/Clans/Clan.cs`
- `Hagalaz.Services.GameWorld/Services/ClanService.cs`
- `Hagalaz.Game.Scripts/Widgets/Clans/ClanSetup.cs`
- `Hagalaz.Data/Entities/ClansSetting.cs`

## How to implement

1. Extend clan membership/rank and settings only through the existing Clan service and data entities. Treat citadel land/weekly resources as a distinct missing feature, not as a flag in chat membership.
2. For citadel rollout, verify 742 room, portal and plot assets first. Start with owner access/visit permissions, then one persistent resource plot; reuse existing map/skill services.

## Verification criteria

- Rank changes are authorized; member leave/rejoin preserves correct rights; citadel access requires proper clan/membership; weekly resources cannot be redeemed twice.
- Resolve exact 742 interface IDs, timings, data and animations with client/cache fixtures and focused server tests.

## Evidence

- [Historical reference](https://wiki.darkan.org/Update%3AClan_Citadels_are_Here%21)
- Follow [implementation conventions](implementation-conventions.md) before adding state, handlers or persistence.
