# Spec Delta

## Purpose

Provides an accessible workspace for inspecting sprite metadata and decoded frames, and replacing a complete sprite through the existing cache API.

## ADDED Requirements

### Requirement: Sprite detail workspace
The sprite detail route MUST present metadata and frame count from the selected sprite's API response, with distinct loading, empty, and failure feedback.

#### Scenario: Sprite metadata is loaded
- **WHEN** an administrator opens a sprite detail route and the API returns metadata
- **THEN** the workspace displays the sprite ID, dimensions, and actual frame count

#### Scenario: Sprite has no usable frames
- **WHEN** metadata reports no usable frames
- **THEN** the workspace displays an empty-frame state and does not present a nonexistent frame as selected

#### Scenario: Metadata request fails
- **WHEN** the API cannot load the selected sprite metadata
- **THEN** the workspace displays an error and does not attribute stale metadata to the selected sprite

### Requirement: Frame browsing and preview
The workspace MUST let administrators browse decoded frame thumbnails and view a selected PNG preview without eagerly requesting every frame.

#### Scenario: Select a frame
- **WHEN** an administrator selects an available frame
- **THEN** the workspace requests and displays that frame, its zero-based index, and its actual dimensions

#### Scenario: Browse a long frame list
- **WHEN** an administrator views a frame list containing many frames
- **THEN** the workspace loads thumbnails for visible or near-visible frames on demand and does not request every frame image at once

#### Scenario: Rapid frame selection
- **WHEN** an administrator selects another frame before a prior frame request completes
- **THEN** the preview eventually displays the most recently selected frame and does not identify an older response as the current selection

#### Scenario: Select frame outside updated range
- **WHEN** the available frame count changes and the selected index is no longer valid
- **THEN** the workspace selects a valid frame or presents the empty-frame state

#### Scenario: Navigate frames by keyboard
- **WHEN** an administrator focuses the frame browser and uses its documented keyboard navigation
- **THEN** the selected frame changes and the selection is exposed to assistive technology

### Requirement: Preview controls and download
The preview MUST preserve image proportions and transparency while offering accessible zoom, fit, and background controls and a PNG download for the selected frame.

#### Scenario: Change preview presentation
- **WHEN** an administrator changes zoom or preview background mode
- **THEN** the displayed frame updates without changing the source image data

#### Scenario: Download selected frame
- **WHEN** an administrator requests a PNG download
- **THEN** the currently selected decoded frame is downloaded as PNG

### Requirement: Whole-sprite replacement
The workspace MUST allow replacement only through the existing whole-sprite image operation and MUST clearly identify its scope before confirmation.

#### Scenario: Preview a valid replacement
- **WHEN** an administrator selects a valid local PNG
- **THEN** the workspace validates it and presents the original and proposed replacement before upload

#### Scenario: Reject an invalid replacement
- **WHEN** an administrator selects a file that is not a valid supported PNG image
- **THEN** the workspace explains the validation failure and does not submit the file

#### Scenario: Cancel replacement confirmation
- **WHEN** an administrator cancels the explicit whole-sprite replacement confirmation
- **THEN** no mutation request is submitted and the current sprite remains unchanged

#### Scenario: Replace the complete sprite
- **WHEN** an administrator confirms the replacement
- **THEN** the existing API receives the replacement and the interface describes the operation as replacing the entire sprite, not one frame

#### Scenario: Replacement succeeds
- **WHEN** the server confirms replacement
- **THEN** the workspace reports the server result and refreshes metadata and the displayed frame

#### Scenario: Replacement fails
- **WHEN** the server rejects or fails the replacement
- **THEN** the workspace displays failure feedback, preserves the proposed preview for retry, and does not report success

### Requirement: Sprite identity and preview resource ownership
The workspace MUST associate metadata, frame responses, and replacement results with the current sprite, and release temporary preview resources when they are superseded or the workspace is destroyed.

#### Scenario: Sprite ID changes during a request
- **WHEN** navigation changes the sprite ID while metadata or frame requests are outstanding
- **THEN** responses for the previous ID are not displayed as data for the new sprite

#### Scenario: Temporary preview is replaced or released
- **WHEN** a preview object URL is superseded or its owning workspace is destroyed
- **THEN** the temporary URL is revoked

### Requirement: Existing sprite workflows remain available
The existing sprite detail URL, standalone sprite utility route, and current cache API operations MUST remain compatible.

#### Scenario: Open existing detail URL
- **WHEN** an administrator navigates to `/cache/types/sprites/:id`
- **THEN** the sprite workspace opens without requiring a different route

#### Scenario: Use standalone sprite utility
- **WHEN** an administrator opens the standalone sprite utility
- **THEN** its existing search and supported image operations remain available
