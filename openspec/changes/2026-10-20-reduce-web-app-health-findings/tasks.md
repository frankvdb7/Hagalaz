## 1. Characterize and simplify request boundaries

- [ ] 1.1 Add the shared cache error mapper and use it from sprite/types request runners without changing error precedence or loading/error state transitions.
- [ ] 1.2 Extract launcher command-shape validation from `getHandler` while preserving argument consumption, warning messages, handler lookup, and trailing-argument dispatch.
- [ ] 1.3 Add focused Vitest coverage for cache request success/failure state and launcher invalid/valid command dispatch.

## 2. Decompose the types manager surface

- [ ] 2.1 Move the types read cards and their existing forms/signals/service calls into a focused standalone read-panel component.
- [ ] 2.2 Move the mutation forge and its existing forms/signals/service calls into a focused standalone mutation-panel component.
- [ ] 2.3 Keep the route-level types page as a composition shell and verify all existing labels, controls, service calls, and result/error/loading presentation remain available.

## 3. Verify health and runtime behavior

- [ ] 3.1 Run the full Fallow report and changed-file audit; confirm the scoped health findings are gone without broad threshold/ignore changes.
- [ ] 3.2 Run `pnpm run lint:biome`, focused admin Biome lint, web/admin tests, and web/admin production builds.
- [ ] 3.3 Run `git diff --check`, validate the OpenSpec change, and inspect the final diff for unrelated changes.
