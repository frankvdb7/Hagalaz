# Design

## Context

`TypesManagerPage` already uses `CacheCs2Store` to paginate ID ranges and route numeric lookups. The CS2 detail component wraps the generic definition viewer. Both Cache service reads currently return summary DTOs. The generic range provider can synthesize definitions for missing cache entries.

## Goals / Non-Goals

Use one revision-742 decoder for cache reads and keep the existing Cache Studio route and request state. The browser receives structured disassembly and renders it as text. A script interpreter or compiler is outside scope.

## Decisions

- **Decode in the Cache library.** Correct `Cs2DefinitionCodec` using `ClientScript.java`, `CS2InstructionInfo.java`, and the opcode table in the local revision-742 client. Use a single operand-kind mapping for decode and encode. The client reads string, signed 64-bit, signed 32-bit, or unsigned-byte operands depending on the opcode. The current three-opcode decoder skips operand bytes for other instructions and can lose alignment. Validate the instruction count and footer boundary before returning a definition. Use stable numeric opcode IDs. Any displayed mnemonic must have a verified mapping.
- **Reuse the existing authenticated routes.** Keep the range response as a summary. Add structured instruction rows and switch tables to the single-ID response, keeping existing summary fields and giving the Angular detail read its own local DTO. Include an operand kind and serialize signed 64-bit operands as decimal strings to preserve precision through JSON and JavaScript. Putting disassembly in the range response would send instruction bodies for every row.
- **Identify missing scripts by cache membership.** Use index 12, archive ID equal to script ID, member 0. Omit synthetic gap rows from the CS2 summary page while keeping ID-range pagination. Report a missing entry as 404 and map invalid cache data through the existing Cache service error handling. Instruction count and script name are data, so neither can serve as a presence check.
- **Keep request ownership with the existing stores.** The Cache service owns lookup and decoding. The CS2 store owns the active list and detail requests, and the detail route ID selects the active record. Reuse the existing resource cancellation and retry behavior. The dedicated view owns the metadata and instruction layout. The shared JSON viewer cannot show typed operands and switch tables in that layout.

## Risks / Trade-offs

- The existing encode/decode round trip can pass when both sides use the same incorrect layout. Use independent revision-742 fixtures or expectations from the local client corpus, including byte and int operands in the same stream.
- An ID window can contain gaps. Keep the paginator based on the existing archive ID extent so empty or short pages do not prevent reaching later IDs.

## Migration Plan

Deploy the Cache API response extension before the admin page consumes it. The change requires no cache rewrite or database migration. The admin page can be rolled back independently because the API keeps its existing summary fields.
