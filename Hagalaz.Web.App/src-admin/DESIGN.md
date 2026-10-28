---
version: alpha
name: Hagalaz World Admin
description: Dark Material interface for administering Hagalaz game data.
colors:
  primary: "#ffb300"
  on-primary: "#432c00"
  primary-container: "#51432c"
  on-primary-container: "#ffdeac"
  surface: "#0b111b"
  surface-low: "#161f2d"
  surface-container: "#192433"
  surface-high: "#1e2a3a"
  surface-highest: "#29394b"
  on-surface: "#edf1fa"
  muted: "#b9c9dd"
  outline: "#8293aa"
  outline-variant: "#39485c"
  secondary-container: "#283b4e"
  on-secondary-container: "#d4e7fa"
  error: "#ffb4ab"
  on-error: "#690005"
  error-container: "#93000a"
  on-error-container: "#ffdad6"
  cyan: "#7bd8e9"
  emerald: "#7adcb0"
  violet: "#c7adff"
  ember: "#ff9993"
typography:
  headline-large:
    fontFamily: Semplicita Pro
    fontSize: 32px
    fontWeight: 400
    lineHeight: 40px
  headline-medium:
    fontFamily: Semplicita Pro
    fontSize: 28px
    fontWeight: 400
    lineHeight: 36px
  title-large:
    fontFamily: Semplicita Pro
    fontSize: 22px
    fontWeight: 400
    lineHeight: 28px
  body-large:
    fontFamily: Semplicita Pro
    fontSize: 16px
    fontWeight: 400
    lineHeight: 24px
  body-medium:
    fontFamily: Semplicita Pro
    fontSize: 14px
    fontWeight: 400
    lineHeight: 20px
  label-large:
    fontFamily: Semplicita Pro
    fontSize: 14px
    fontWeight: 500
    lineHeight: 20px
spacing:
  xs: 4px
  sm: 8px
  md: 16px
  lg: 24px
  xl: 32px
rounded:
  card: 16px
  field: 12px
  button: 12px
  full: 9999px
components:
  page:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.on-surface}"
    typography: "{typography.body-medium}"
  outlined-card:
    backgroundColor: "{colors.surface-low}"
    textColor: "{colors.on-surface}"
    rounded: "{rounded.card}"
  primary-button:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.on-primary}"
    typography: "{typography.label-large}"
    rounded: "{rounded.button}"
  outlined-field:
    textColor: "{colors.on-surface}"
    rounded: "{rounded.field}"
  error:
    backgroundColor: "{colors.error-container}"
    textColor: "{colors.on-error-container}"
  supporting-text:
    textColor: "{colors.muted}"
    typography: "{typography.body-medium}"
  quick-action:
    backgroundColor: "{colors.surface-high}"
    textColor: "{colors.on-surface}"
  quick-action-hover:
    backgroundColor: "{colors.surface-highest}"
    textColor: "{colors.on-surface}"
  cyan-accent:
    textColor: "{colors.cyan}"
  emerald-accent:
    textColor: "{colors.emerald}"
  violet-accent:
    textColor: "{colors.violet}"
  ember-accent:
    textColor: "{colors.ember}"
---

# Hagalaz World Admin design

## Overview

The admin app is a staff workspace for inspecting and managing game data. Its dark navy surfaces and amber Hagalaz mark connect it to the game while keeping forms, data and operational feedback readable. Login and the workspace share a backdrop; operational content stays on solid surfaces.

The YAML tokens describe the dark theme. `src-admin/styles.scss` initializes Material and `src-admin/portal-theme.scss` owns admin role and component overrides. The palette and bundled fonts come from `src/theming/`. Keep implementation and these tokens in sync.

## Colors

Use amber primary for the main action and active navigation, with dark on-primary text. Navy surface roles distinguish the canvas, cards and raised controls. Use on-surface for primary text, muted for supporting text and outline-variant for quiet boundaries.

Cyan, emerald, violet and ember distinguish portal categories and statuses. Pair them with text or icons; never imply that a live metric or service is connected when its data is unavailable. Use Material error roles with written validation and failure messages.

## Typography

Use bundled Semplicita Pro for brand and interface text, with Material headline, title, body and label roles. The YAML lists commonly used roles; the generated Material theme supplies the rest. Use sentence case for actions and preserve readable labels, errors and data. Material Symbols supply interface icons.

## Layout

Use a persistent Material drawer on desktop and an accessible overlay drawer on narrow screens. The toolbar contains breadcrumbs, workspace search and account controls. Main content uses responsive padding; wide data scrolls inside its own region.

The overview centers a 64rem column with a hero, metric cards, activity and quick actions, followed by health and recent activity. Stack panels at narrow widths. Sign-in uses a centered, constrained form and a compact layout on phones.

## Elevation & Depth

Outlined cards use the low surface role, outline-variant border and shared shadow `0 20px 70px rgb(0 0 0 / 22%)` through a Material override. Outlined fields use the outline role. The overview removes the shadow from its compact panels. Use surface contrast and outlines to separate content. The shared background is decorative and must not reduce content readability.

## Shapes

Outlined cards use 16px corners; outlined fields and filled actions use 12px corners. Default fields are 64px high and filled actions 52px high. Compact controls may use local Material overrides where the layout requires them.

## Components

- **Navigation:** Use the existing drawer, toolbar, route-aware links and account menu. Give icon-only controls accessible names and visible focus.
- **Actions and forms:** Use Material buttons and fields, descriptive labels, inline validation and visible pending states. Preserve disabled states and prevent duplicate submissions where needed.
- **Cards and data:** Group related information in Material cards. Distinguish loaded, empty, unavailable and error states with text. Archive counts come from the existing cache store.
- **Assets:** Reuse the lightning logo, login backdrop and overview orbit illustration from `src/assets/`. Keep decorative imagery out of data-entry and result regions.

## Do's and Don'ts

- Use public Angular Material theme and component overrides; use shared CSS variables for values Material does not expose.
- Reuse existing routes, stores, fonts and icons before adding components or visual systems.
- Preserve keyboard access, focus visibility, error messages and narrow-screen usability.
- Do not invent telemetry, authentication states or backend capabilities to fill a design.
- Do not copy public-app translucent panels or launcher chrome into the admin workspace.
