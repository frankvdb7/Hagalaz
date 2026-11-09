## Why

The login and signed-in admin pages use different Material component defaults despite sharing a visual identity.

## What Changes

Apply the login's dark surfaces, rounded fields and cards, and amber primary actions consistently across the admin application using its existing Material theme.

Match the supplied admin sign-in reference while keeping its existing authentication and validation behavior.

## Capabilities

### New Capabilities
- `admin-shared-theme`: Shared visual treatment for admin Material components.

### Modified Capabilities
None.

## Impact

The Angular admin theme and login styling. Existing Material components and theme APIs are reused.

## Acceptance criteria

- A1: Login, signed-in pages, and overlays share Material card, outlined field, and primary button styling.
- A2: Disabled, error, and focus states remain distinguishable and existing operations remain unchanged.
- A3: The admin production build, lint, and representative desktop and narrow browser checks pass.
- A4: The login matches the supplied 1648×954 reference and remains usable at 390×844 without horizontal overflow. Existing email, password, captcha, password-visibility, and disabled-submit behavior remains unchanged.

## Non-goals and stop conditions

No authentication, backend, public app, dependency, or navigation changes. Do not redesign individual workflows or add a second theme system.
