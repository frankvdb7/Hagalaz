# Spec Delta

## Purpose

Run cache-backed widget callbacks locally in the Admin Widget Detail preview.

## ADDED Requirements

### Requirement: Load executable widget scripts
The Cache service SHALL produce readable, executable RuneScript for supported revision-742 instructions. Widget Play SHALL load callback programs and their direct and transitive callees from the Cache service before running them.

#### Scenario: Load a widget with nested calls
- **WHEN** a widget callback calls another cached script
- **THEN** the preview loads the called script and can resolve its arguments and return values locally

### Requirement: Execute widget events locally
Widget Play SHALL interpret the loaded RuneScript in the browser and apply supported widget effects to the temporary canvas.

#### Scenario: Play and interact
- **WHEN** the scripts load and the administrator starts Play or dispatches a supported pointer or manual event
- **THEN** the matching callbacks run and successful effects appear without a server execution request

#### Scenario: Stop playback
- **WHEN** the administrator stops playback or changes widgets
- **THEN** temporary state is discarded and any late load cannot change the next preview

#### Scenario: Unsupported operation
- **WHEN** a callback hits an unsupported instruction, unavailable script or execution limit
- **THEN** the preview reports the failure and preserves the previous state
