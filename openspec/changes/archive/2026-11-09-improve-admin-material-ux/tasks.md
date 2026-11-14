## 1. Material shell and entry screens
- [x] 1.1 Replace decorative admin styling with the existing Material theme and responsive shell, sign-in, and denied screens. Maps to A1 and A2.

## 2. Cache workflows
- [x] 2.1 Organize Types and Sprites into Material browse/edit workflows with clear field labels, named results, and responsive layouts. Maps to A1 and A2.
- [x] 2.2 Guard invalid and pending submissions and provide accessible loading, errors, and edit confirmations. Maps to A3.

## 3. Verification
- [x] 3.1 Run the production admin build, Biome, OpenSpec validation, and browser checks for desktop, narrow layouts, and operation feedback. Maps to A4.

## 4. NgRx request state
- [x] 4.1 Move Types and Sprites request state and operations into component-scoped NgRx signal stores, retaining local form state and preview URL cleanup. Maps to A3 and A5.
- [x] 4.2 Verify the store migration with the admin build, existing tests, lint, audit, and fixture browser read/edit/preview/navigation flows. Maps to A4 and A5.

## 5. Store regression tests and coverage
- [x] 5.1 Add direct tests for both stores using the real HTTP services with Angular's testing backend: successful operations, request arguments, failures and retries, pending guards, and page-scoped state. Maps to A3 and A5.
- [x] 5.2 Test the Sprites page's preview URL replacement and destruction cleanup with store-backed Blob responses. Maps to A3 and A5.
- [x] 5.3 Enable the existing Vitest runner's matching coverage provider, run the admin tests with coverage, inspect coverage of the new stores and shared error policy, and run lint/audit checks. Maps to A4 and A5.
