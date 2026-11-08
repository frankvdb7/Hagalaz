# Design

## Reference measurements and assets
Target: supplied codex-clipboard-07acb157-b7a8-4416-9c82-b0e9410197d3.png, 1651x953. Content begins after the existing sidebar and toolbar. Header is about140px high, content grid has a flexible main panel, 354px inspector and24px gap. Tabs are56px tall; cards use1px blue-gray borders,8px corners and24px padding. Header thumbnail112px, editor fields about40px high. Smaller viewports stack the inspector and form columns.

Assets: existing supplied Hagalaz SVG logo and avatar belong to the unchanged shell; item images come from the authenticated item sprite service and the existing shared 3D viewer. No generated imagery is needed. Existing Semplicita Pro typography and Material Symbols are the source design system. Do not fabricate a skeleton illustration or use mock item data.

## Mutation ownership
The user explicitly rejected a field-specific UpdateBasicFields API. Load a complete GetItemDefinitionDto and PUT an UpdateItemDefinitionDto at /types/items/{id}/definition. The item feature explicitly maps all stored fields to and from ItemType; the cache consumer interface remains read-only. There is no partial-update DTO or patch parser. ItemTypeCodec remains the single opcode owner and retains unmodeled/legacy opcode payloads on decode for re-encoding. Full saves retain these payloads from the persisted source. Name/base-value validation remains at the protected mutation endpoint; noted and other derived properties are read-only projections.

Full-save verification exposed lossy encoding: member status was suppressed when a note counterpart existed, template fields were omitted, head IDs used short writes despite big-smart reads, and paired settings were skipped when one value was zero. The raw codec now round-trips stored fields independently of template/provider resolution. Read-only probes for real IDs0,20139,20769 compare every serialized field before and after encoding with no differences. No real cache writes were performed.

Item duplication supplies its index, source mapping and archive member limit through ItemTypeData. CacheApi.AppendArchiveMemberCopy directly allocates an append position and copies under the existing write lock. No callbacks, item constants or item-specific operations exist in CacheApi. Duplication is an exact byte copy at CacheApi's write lock. Add one append-copy member operation because ReplaceArchiveMember cannot add a member or archive. Select the next ID from actual reference mappings while locked, copy source bytes unchanged, rebuild only the destination archive and let CacheWriter update checksums, versions and reference mappings through the same write mechanism. CacheWriter receives an explicit updated table for this existing operation; no worker, retry/rollback framework or second lock. Existing filesystem writes retain their existing crash/failure guarantees, not database transaction claims.

CacheItemsStore remains the mutation/request owner. The page owns one linked draft and registers it with CachePendingChangesService. Save and duplicate use CacheWriteDialogComponent; tests approve in isolated MemoryFileStore only. Duplicate starts from persisted source and is disabled while unsaved changes exist. Description remains disabled with a clear cache-format explanation; noted is read-only and refers to Variants. Preview, raw JSON and other tabs retain live data.

## Verification boundary
Use automated tests and builds. The user's previous instruction to skip browser checks overrides the skill's browser gate. design-qa.md must record blocked visual verification, not passed fidelity. Do not claim a pixel-verified recreation or deploy/share.


## Inspector and asset placement
The reference's compact inventory sprite is shown with a working zoom control. The existing interactive 3D model/particles/appearance viewer remains in an expandable inspector below it. This preserves the reference panel hierarchy and all existing viewer features without inventing model images. Technical values come from the full definition. No font/icon dependency or generated asset was added because supplied application assets cover the reference.


## Completed validation
239 admin tests, 73 cache-service tests, 44 targeted codec/writer/snapshot tests and 35 shared-viewer tests passed. Both Angular production applications built successfully. The Aspire cache resource rebuilt successfully and reported healthy. Read-only round trips for real item IDs 0, 20139 and 20769 retained every serialized field. Tests mutated isolated stores only.

Regression coverage includes full-definition model and legacy-byte preservation, template/counterpart fields, extended head IDs, same/new archive duplication, concurrent unique allocation, invalid input, failed-save draft retention, pending navigation, sprite zoom and raw-definition navigation. Existing dependency audit/pruning and test warnings remain; no new source compiler warning was introduced. Browser/visual QA is blocked by the user's explicit waiver and is recorded in Hagalaz.Web.App/design-qa.md. No visual fidelity claim is made.


### Duplication boundary refactor verification
All 73 cache-service tests passed after moving item layout into the endpoint, including exact-copy same/new archive boundaries and concurrent unique allocation. Test builds used isolated artifact output because the running service locks its DLLs. No source compiler warnings were introduced. The running service was left unchanged; this refactor is built and tested but awaits its next restart.

The destination callback was removed per user instruction. AppendArchiveMemberCopy takes an explicit archive member limit, allocates after the last mapped member and rolls into the next archive under the existing write lock. All 73 cache-service tests pass after this change, with no source compiler warnings. Running service remains unchanged until restart.

### Ordinary item encoding
PreservedOpcodes is internal metadata on the decoded ItemType. The update endpoint applies the complete DTO onto that decoded item before calling the existing one-argument Encode. Cache implementations are not REST contracts. IItemTypeCodec remains a marker interface; no source-specific encoder or alternate encoding path is needed.

Verification: 73 service tests and 7 focused item codec tests passed. PreservedOpcodes_AreExcludedFromJsonInBothDirections verifies metadata is neither exposed nor accepted via JSON. FullDefinitionEncode_RetainsUnmodeledPayloadsAndExtendedHeadIds uses ordinary Encode; SaveFullDefinition_ChangesModelFieldsAndPreservesLegacyBytes covers endpoint preservation. Existing test nullability and NuGet warnings remain. No service restart or real cache mutation performed.
