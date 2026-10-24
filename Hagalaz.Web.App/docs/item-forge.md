# Item Forge

The item detail route is read-only. It uses the existing item API, which returns cache definitions. Cache base value is not a market quote or the server's trade price. Equipment metadata does not include server gameplay bonuses. Quantities, ownership and charges belong to runtime item instances.

Note, lending and bought item/template references come directly from decoded fields. A negative reference is absent; zero is a valid ID. No adjacency or reciprocal relationship is inferred. Inventory model IDs are not sprite IDs. The page reserves a preview area because there is no rendered item-image endpoint.

The decoder also supports worn/head models, recolors, textures and stack-amount model variants. These need their own understood contracts before exposure. See `docs/revision-742/reference/deep-item-relationships.md` at the repository root.

## Smallest safe mutation follow-up

1. Select a small set of supported fields, such as name and ordered inventory actions. Define a typed patch DTO with explicit omitted-versus-null semantics. Do not accept arbitrary JSON or ExtraData keys.
2. Reuse CacheAdminPolicy and require an explicit item-write permission. Validate the ID exists in the populated reference table, field lengths/encoding, action slot count and allowed null slots. Validate any references against real target definitions and template semantics.
3. Require a cache revision or content hash to reject stale writes. Load the original decoded item and preserve every unsupported field/opcode. Prove decode/encode/decode round trips against representative revision-742 bytes, including unknown data, derived note/lending/bought forms and sparse actions. If the codec cannot preserve a definition, reject its mutation until preservation is implemented.
4. Use the existing cache writer and invalidation ownership. Define atomic write/error behavior before enabling the UI. Never write projected note or lending values back over original encoded data accidentally.
5. Test authorization failures, invalid IDs and references, invalid actions, stale revisions, round-trip preservation, write failures, provider invalidation and a subsequent GET. Only then add a Signal Form with validated changes, confirmation, pending guards and visible success/error results.

This change adds no mutation route or generic mutation engine.
