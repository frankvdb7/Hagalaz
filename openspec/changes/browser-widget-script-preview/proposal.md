# Proposal

## Why

Widget Play sends each event to the Cache service for execution. Running the script in the browser lets the preview handle pointer events and apply widget changes locally.

## What Changes

- Have the Cache service decode CS2 and emit a readable, executable RuneScript subset for widget callbacks and their callees.
- Load the program when Play starts. Interpret it in JavaScript and keep widget and simulated variable state in the active preview.
- Route the existing open, pointer and manual events through the local runtime. Retire the server preview execution route once the browser uses the new path.

## Capabilities

### New Capabilities

- `admin-browser-widget-runtime` covers loading and executing widget scripts locally in Play mode.

### Modified Capabilities

None. The active `widget-script-preview` spec now describes browser execution. This change defines the executable program and browser runtime contract.

## Impact

Cache service script responses and the Angular Widget Detail preview. Reuse the revision-742 decoder, callback metadata, Play controls and widget renderer.

## Acceptance and stop conditions

Use real cache fixtures to verify local callbacks, nested calls and visible widget updates after scripts load. Confirm that subsequent events send no execution requests and Stop restores the baseline. General JavaScript execution, arbitrary browser API access and full game-client parity are outside scope.
