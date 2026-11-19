# Spec delta

## ADDED Requirements

### Requirement: Compact model previews recover from render failures

The admin site SHALL offer an accessible retry when the compact model renderer fails during view creation or change detection. Retrying SHALL recreate the renderer using the loaded model data and preserve the surrounding editor and draft.

#### Scenario: Renderer fails and recovers

- **GIVEN** an administrator has loaded a model in a compact preview
- **WHEN** the renderer throws during view creation or change detection
- **THEN** the preview announces the failure and offers a keyboard-accessible retry without exposing raw error details
- **AND** a successful retry restores the renderer without repeating model or texture requests

#### Scenario: Model request fails

- **WHEN** the preview's model request fails
- **THEN** the existing request error and request retry remain available without showing the render failure fallback

#### Scenario: Dirty editor survives preview failure

- **GIVEN** an administrator has unsaved edits in Item Forge
- **WHEN** the compact preview succeeds or displays its render fallback and the administrator attempts to leave
- **THEN** navigation still requires confirmation to discard the draft
- **AND** cancelling navigation retains the edits without issuing a mutation
