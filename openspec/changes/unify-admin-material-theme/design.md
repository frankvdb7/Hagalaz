## Design

The admin Material theme is the single owner of shared color and component tokens. `portal-theme.scss` is included by the global admin stylesheet. Its public Material overrides map outlined cards to the low surface-container role and shared elevation, fields and filled actions to the login's dimensions, and filled action colors to the primary/on-primary roles. The Material system variables remain the source used by components. Component overrides and non-Material accent, backdrop and shadow variables are defined at the theme root and inherited by pages and overlays. Compact controls can retain their local size overrides. Keep the application of the login backdrop and form-specific spacing local. Use a solid amber button rather than a CSS gradient so Material continues to own interactive and disabled states throughout the app. Existing contextual success and error card overrides remain local.

Use the supplied sign-in screenshot as the login reference. The backdrop is a generated image asset; the Hagalaz logo and Material Symbols remain the existing brand assets. Keep stacked, accessible labels and the wide hCaptcha widget inside the existing Angular Material form and preserve its current validation, loading, password-visibility, and disabled-submit behavior.

This is a presentation-only change; existing forms and stores own all state and transitions. No retry or state owner changes.

## Verification

Build and lint the admin app, validate this change, and inspect login and a representative signed-in page at desktop and narrow sizes when available. Verify disabled and error presentation without submitting credentials.
