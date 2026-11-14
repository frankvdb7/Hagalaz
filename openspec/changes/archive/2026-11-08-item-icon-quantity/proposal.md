# Proposal

## Why

Rendered item icons always show a single unit with no label, while the client draws stack models (for example coin piles) and a quantity label. Interfaces such as the backpack configure this through CS2 (script 1613: quantity mode 2).

## Goal

Let callers render an item icon for a quantity, with the client's stack model and quantity label.

## What Changes

- `GET /types/items/{id}/image` accepts `quantity` (default 1, -1 or greater) and an optional `quantityMode` (0, 1 or 2) that selects a client mode.
- The quantity selects the item's stack variant model as the client does.
- The quantity label is drawn with the client `p11_full` font, colours and K/M thresholds. By default only stackable items with a quantity above zero are labelled, including quantity one; ordinary items are never labelled unless a client mode is requested.
- A label that needs a font the cache does not contain returns 422.

## In scope

Stack variant selection, the quantity label for modes 0–2, label scaling for larger icons, validation and tests.

## Non-goals

Player appearance recolouring, other fonts, localised K/M suffixes, and matching the previous Grand Exchange GIFs.

## Acceptance criteria

1. Quantities at or above a stack amount render the stack variant model.
2. Labels follow the client modes, colours and thresholds; mode 0 never draws one.
3. Invalid `quantity` or `quantityMode` values return 400; a needed but missing font returns 422.
4. Existing icon behaviour is unchanged for `quantityMode=0`.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `cache-item-images`: item icons can be rendered for a quantity with the client stack model and quantity label.

## Impact

Item image endpoint, `ItemIcons`, the cache renderer (`ClientFont`, quantity helpers), font fixtures and tests. Reuses `SpriteTypeCodec` and the cache name lookup.
