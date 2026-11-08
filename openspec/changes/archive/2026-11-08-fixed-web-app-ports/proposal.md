# Stable Aspire web app ports
## Why
The web app endpoints fix targetPort only, so Aspire assigns random public URLs.
## What Changes
Set the main web app public HTTP port to 4400 and admin to 4410. Allow Aspire to allocate internal ports and inject PORT for the existing Angular scripts.
## Acceptance criteria
1. Aspire advertises main/admin HTTP ports 4400/4410 respectively, retaining proxying and existing PORT injection.
2. AppHost model regression tests/build pass without port collisions in the model.
## Non-goals
No Angular standalone port changes, gateway/auth changes, deployments or forced restart of the existing distributed session.
## Capabilities
### New Capabilities
- `local-web-endpoints`: stable web-app URLs in Aspire.
### Modified Capabilities
None.
## Impact
AppHost endpoint annotations and focused model tests only.
