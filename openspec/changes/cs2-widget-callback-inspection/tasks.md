# Tasks

## 1. Widget callback inspection

- [x] 1.1 Decode revision-742 widget callbacks in the Cache service and return typed callback metadata with preview bytes. Run endpoint fixture tests for typed arguments, event order, trigger filters, empty and truncated tails.
- [x] 1.2 Render server-provided callback rows and numeric script links to the selected component's inspector. Run Angular tests for component changes, callback ordering, empty and unavailable states, and navigation to the existing CS2 detail route.

## 2. CS2 source and component references

- [x] 2.1 Verify the initial opcode semantics against the revision-742 client and add a pure pseudocode analyzer in the Cache service. Run focused tests for expression reconstruction, a direct call with a known signature, branch and switch targets, and raw fallback when a region cannot be reconstructed.
- [x] 2.2 Render the server-provided source beside disassembly and render links for verified static widget operands. Run fixture tests for an exact widget and component target, dynamic and unverified operands, and a script containing both reconstructed and raw output.
- [x] 2.3 Resolve component links in the existing widget detail route. Run navigation tests for a direct link, a changed component ID in the same widget, selection highlighting, missing destinations, and back navigation.

## 3. Integration

- [x] 3.1 Build the Cache service and admin and verify in a signed-in session that a pinned-cache component opens its callback's source and that a verified widget reference opens and highlights the addressed component.
