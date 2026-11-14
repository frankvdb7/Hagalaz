# Proposal

## Why

Widget detail shows cached layout and callback source. Administrators need to see how supported callbacks change the widget while they interact with it.

## What Changes

- Add an interactive preview mode that runs cached open callbacks when started, the matching callback on a preview click, and selected callbacks from the Logic inspector.
- Load revision-742 programs from the Cache service and run them in the browser. Apply successful visual changes to the preview and show which script and instruction blocked an unsupported run.
- Keep the preview state temporary and resettable. GameWorld sessions, network effects, automatic game ticks, and cache writes are outside scope.

## Capabilities

### New Capabilities

- `admin-widget-script-preview` covers running widget callbacks and displaying their effects in the widget detail preview.

### Modified Capabilities

None.

## Impact

The Cache service supplies programs under its existing authorization policy using the revision-742 CS2 codec and cached callback decoder. The admin reuses the widget renderer and Logic inspector.

## Acceptance and stop conditions

Verify open, click, and manual events with fixtures that visibly change a component. Verify a failed event leaves the prior preview intact, and reset restores cached state. The first version ends at supported widget and simulated variable effects.
