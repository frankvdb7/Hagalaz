## Context

`Character` assigns `ICharacterContextProvider.Context` in its constructor
before calling `IDefaultCharacterScriptProvider.GetAllScripts`. Microsoft DI
creates constructor dependencies before that constructor body runs. The
provider currently accepts `IEnumerable<IDefaultCharacterScript>`, which makes
DI instantiate every default script while the provider itself is resolved.

## Decision

Make `DefaultCharacterScriptProvider` depend only on `IServiceProvider` and
resolve the default-script collection inside `GetAllScripts`. The existing
concrete-type lookup remains in place so the provider keeps returning the
scoped concrete registrations used by the plugin.

This leaves `Character`'s explicit provider dependency and its current context
assignment order intact. It moves only the script instantiation boundary to the
point where the context is ready.

## Risks

The provider must not be called before a character context exists. That is
already the provider's use contract, and the regression test will exercise the
safe construction sequence explicitly.
