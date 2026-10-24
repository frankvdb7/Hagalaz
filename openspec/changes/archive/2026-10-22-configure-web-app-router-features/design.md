# Design

Use the existing `provideRouter` calls in the public and admin bootstraps and add the same two router features to each. Keep provider ownership at each app bootstrap, with no new helper or abstraction. The platform navigation feature is experimental in Angular; it is included because it was explicitly requested. Use the stable, non-deprecated `withAutoCleanupInjectors()` API for injector cleanup.
