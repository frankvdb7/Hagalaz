## Design

Reuse the existing portal shell, Material controls, typography, semantic theme colors, archive-count store and routes. Adjust the overview to the screenshot's compact hero, four metric cards, activity and action panels, and two lower panels. Use a dedicated orbit illustration and the existing logo. Keep the absent telemetry state truthful; do not add a nonfunctional time filter. The shell keeps its accessible mobile drawer, toolbar controls, and account menu.

The workspace canvas reuses the login background through the shared admin theme variable. Solid Material surfaces preserve legibility of operational content.

Existing stores own request state and retry; this presentation change adds no state or retry owner. Existing theme variables and public Material overrides remain authoritative. Direct page layout is simpler than a new component framework.

## Verification

Build and lint the admin, validate the change, and compare the rendered reference and narrow layouts at matching dimensions where access permits. Verify key navigation and archive count states without modifying real data.

The current implementation was compared at 1644×957 in an isolated fixture. Its mobile drawer and navigation were checked at 390px, and the admin build, lint and tests passed. The orbit detail and hero treatment still differ from the reference; the fixture did not verify live archive loading. The design QA report records these gaps, so the tasks remain open.
