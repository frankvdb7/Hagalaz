# Design

## Context

The Cache service decodes CS2 and cached widget callbacks. The existing source builder reconstructs pseudocode for inspection. The Angular widget page owns temporary layout edits and renders them through the existing preview component.

## Goals / Non-Goals

Execute supported callbacks against an isolated widget snapshot and return the next snapshot. See the proposal for scope.

## Decisions

See [Browser widget script preview](../browser-widget-script-preview/design.md) for program loading, event binding, execution, and snapshot ownership.

## Risks / Trade-offs

- Some scripts need GameWorld data or unsupported instructions. Identify the script and instruction and discard the event's tentative changes.
- Component updates can refer to targets outside the loaded widget. Report the unavailable target and preserve the previous state.
- Reset or navigation can race with a response. Check that the response belongs to the current preview before applying it.

## Migration Plan

Deploy the program-loading endpoint before the admin update. No stored data migration is required.
