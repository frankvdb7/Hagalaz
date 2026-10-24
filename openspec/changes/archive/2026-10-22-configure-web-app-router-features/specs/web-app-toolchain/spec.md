## ADDED Requirements

### Requirement: Both web app routers use platform navigation and injector cleanup

The public and admin Angular application bootstraps MUST configure their routers with platform navigation and automatic cleanup for route-created injectors, using the non-deprecated `withAutoCleanupInjectors()` API.

#### Scenario: Public and admin applications bootstrap their routers

- **WHEN** either Angular application is bootstrapped
- **THEN** its router is configured with `withExperimentalPlatformNavigation()` and `withAutoCleanupInjectors()`
