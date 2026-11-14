# Tasks

## 1. Cache decoding and API

- [x] 1.1 Correct revision-742 operand decoding and encoding in `Cs2DefinitionCodec`. Run the focused codec tests with independently verified bytes covering mixed operand widths, switch tables, and a malformed stream.
- [x] 1.2 Extend the guarded single-script response with typed instructions and switch tables. Keep range responses summary-only and omit absent entries. Run Cache endpoint tests for range gaps, missing and malformed scripts, and exact serialization of a signed 64-bit operand.

## 2. Admin Cache Studio

- [x] 2.1 Render ID, script name, and instruction count in the existing CS2 scripts tab. Run a focused Angular regression covering paginated list-to-detail navigation and numeric lookup through the existing route.
- [x] 2.2 Replace the generic CS2 detail view with metadata, instruction rows, and switch tables using the typed detail resource. Run focused Angular tests for direct links, route ID changes, missing scripts, and retry after a read failure.

## 3. Integration

- [x] 3.1 Build the admin and affected .NET projects. In the authenticated admin, open a revision-742 script from the list and by direct URL and verify its metadata and instruction order against the fixture expectations.
