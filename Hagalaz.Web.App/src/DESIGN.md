---
version: alpha
name: Hagalaz
description: Dark fantasy MMORPG website and Electron launcher built with Angular Material.
colors:
  primary: "#ffb300"
  on-primary: "#432c00"
  surface: "#181209"
  on-surface: "#eee0d0"
  panel: "#211b11"
  muted: "#d6c4ac"
  error: "#ffb4ab"
  on-error: "#690005"
typography:
  headline-large:
    fontFamily: Semplicita Pro
    fontSize: 32px
    fontWeight: 400
    lineHeight: 40px
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
  card: 12px
  field: 4px
  pill: 9999px
components:
  page:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.on-surface}"
    typography: "{typography.body-large}"
  card:
    backgroundColor: "{colors.panel}"
    textColor: "{colors.on-surface}"
    rounded: "{rounded.card}"
    padding: "{spacing.md}"
  primary-button:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.on-primary}"
    typography: "{typography.label-large}"
    rounded: "{rounded.pill}"
  secondary-text:
    backgroundColor: "{colors.panel}"
    textColor: "{colors.muted}"
    typography: "{typography.body-medium}"
  error:
    backgroundColor: "{colors.on-error}"
    textColor: "{colors.error}"
---

# Hagalaz design

## Overview

Hagalaz is a fantasy MMORPG community website with an Electron launcher. The main visual reference is `src/`, especially its public navigation, account forms, news cards, and highscores. Amber actions sit above warm brown-black surfaces and game artwork. Keep the artwork visible around content while giving forms and data enough contrast to read.

This document describes the public application and Electron launcher in the [Google DESIGN.md format](https://github.com/google-labs-code/design.md/blob/main/docs/spec.md). It does not certify the current screens as accessible.

The YAML tokens describe the dark theme's intended values. `src/styles.scss` configures Material, `src/theming/colors.scss` owns the palettes, `src/theming/typography/` loads fonts, and `src/tailwind.css` maps Material system tokens to utilities. Keep the tokens and implementation in sync.

## Colors

Amber `#ffb300` is the palette seed and primary tone 80. Use the primary role for the main action, active navigation, and links. Pair filled amber controls with the dark on-primary role. Avoid white text on amber.

Dark surfaces use warm neutrals, including surface tone 6 `#181209` and low container tone 10 `#211b11`. Primary text uses neutral tone 90 `#eee0d0`; supporting text uses neutral-variant tone 80 `#d6c4ac`. The generated palette also contains a yellow-green tertiary family. Let Material assign its semantic roles instead of introducing unrelated accent colors.

Use Material error roles for validation and failed requests. Explain errors in text and preserve the user's input. Color alone must not convey success, error, selection, or disabled state.

Cards currently use 75% opacity over artwork. The solid panel token above is the underlying color, not the composited result. Check contrast against the actual image and overlay. Increase containment through a public Material override when artwork makes text hard to read.

## Typography

Use the bundled Semplicita Pro for both brand and ordinary text. Available weights are regular 400, medium 500, and bold 700. Font files live in `src/assets/fonts/` and load with `font-display: swap`. Use the bundled Material Symbols for interface icons and existing skill artwork for game statistics.

Use Material's headline, title, body, and label roles through existing `font-*` utilities or component typography. The YAML lists the commonly used roles rather than a replacement for the full Material type scale. News titles use headline-large; forms and tables need body and label sizes. Keep hierarchy meaningful with semantic headings.

Public navigation and Material buttons currently render uppercase. Write labels in sentence case and let the theme perform the visual transformation. Keep paragraphs, validation, table headings, and account details in normal case. Do not add a decorative display font or rely on synthetic font weights.

## Layout

The public shell has a sticky header, background artwork, flexible main content, and footer. Use the existing Tailwind `container mx-auto` convention. Its responsive width comes from Tailwind; do not introduce a competing fixed maximum width. Main content has 16px horizontal padding on small screens. Existing page sections commonly use `gap-4` and `py-4`.

Group a page title and short supporting text above its Material card. Account forms use one column and a constrained desktop width. News uses a vertical list of image-backed cards. Highscores uses one card containing the skill selector, table, and paginator. Preserve these recognizable structures when extending a page.

At narrow widths, stack controls and keep actions reachable. Allow wide data to scroll within its own region. Do not hide essential columns or actions solely to fit a screenshot. The current header changes navigation at the small-screen breakpoint; any new navigation must expose usable destinations and an accessible menu control.

The Electron shell keeps its header and footer sticky and hosts content between them. Preserve draggable window areas and interactive window controls when changing launcher layouts.

## Elevation & Depth

Depth comes from artwork behind translucent Material containers. `src/styles.scss` applies 75% surface colors to cards, menus, selects, and the toolbar, with large backdrop blur on selected overlays and cards. Tables and paginators are transparent within their parent card.

Reuse Material elevation and tonal containment. Keep text above the image gradient. Avoid stacking multiple translucent cards inside each other or adding glow around ordinary controls. Verify overlay readability over both bright and dark backgrounds. Artwork and blur must never be required to understand a screen.

## Shapes

Retain Material component shapes: 12px elevated card corners, 4px filled-field top corners, and fully rounded buttons. A field radius does not imply rounding every corner of its filled container. Use the public Material shape overrides for deliberate changes.

Keep images clipped within their cards and skill icons recognizable at small sizes. Use the existing logo and fantasy assets rather than drawing replacement ornaments in CSS.

## Components

- **Navigation.** Use the existing Material toolbar, tab navigation, account menu, and logo. Indicate the active route through the tab state. Give icon-only actions accessible names and visible keyboard focus.
- **Actions.** Use a filled Material button for the main action and text or outlined buttons for supporting actions. Name the operation explicitly. Show pending state and prevent duplicate submission where the operation requires it.
- **Forms.** Use Material form fields, persistent labels, appropriate input types and autocomplete, and inline validation. Account forms currently use filled fields; highscores uses an outlined selector. Keep errors next to the field and provide a form-level message for request failures.
- **Cards and titles.** Reuse `app-card-title` and Material cards. Maintain a clear title, content, and action hierarchy. Keep important text readable without hover.
- **News.** Reuse the image-backed news card, title, author, and relative date. Use an overlay to protect text contrast. Treat the whole linked card as one clear destination.
- **Data.** Reuse the highscores table, skill selector, existing skill icons, number formatting, and paginator. Distinguish loading, empty data, and failed requests in visible text.
- **Feedback.** Reuse the existing loading components and progress indicators. Announce meaningful asynchronous results and associate errors with their controls. Avoid replacing loaded content with an unexplained blank area.
- **Assets and motion.** Reuse `src/assets/images/logo.svg`, `bg.jpg`, slideshow artwork, and `src/assets/icons/`. Existing logo animation and torch effects are decorative. New motion must respect reduced-motion preferences and must not delay an action or obscure content.

The Play page and profile overview contain placeholders. Their incomplete markup does not establish another card system or a finished interaction pattern.

## Do's and Don'ts

- Use Angular Material as the foundation and Tailwind for layout, spacing, sizing, and responsive behavior.
- Set component appearance through public Material Sass theme and override APIs. Keep token expressions and custom properties in SCSS, not HTML.
- Reuse the palette, bundled fonts, logo, game artwork, and shared components before adding visual assets or styles.
- Preserve focus visibility, keyboard access, accessible names, readable contrast, and text feedback. Check desktop and phone widths after UI changes.
- Treat private MDC selectors, inline token overrides, and the global focus-outline removal already present in `src/` as legacy implementation details. Do not copy them into new work. They are not recommended design patterns.
- Do not add arbitrary hex colors to templates, a parallel token system, custom replacements for Material controls, or `!important` component patches.
- Do not apply fantasy decoration to every form or fill data tools with background imagery. Keep operational content easy to scan.
- Keep this document synchronized with deliberate theme changes. Validate it with `npx -p @google/design.md designmd lint src/DESIGN.md` from `Hagalaz.Web.App/`.
