## Design
Use NgRx withState/withProps/withComputed/withMethods to compose page-scoped stores. Shared read features create resources through CacheTypesService, the URL owner. Every kind exports its own store; only supported list/search/mutation features are composed. HTTP resources are the single owner of read values/loading/errors. Resource parameters are signals, with reload for same-parameter reads. Supported mutations use rxMethod, exhaustMap and tapResponse; they update the current resource value on success and publish the backend confirmation.

Dedicated type pages own their store and editor. Shared Material detail layout handles route ID loading, status, metadata, raw inspection, and back navigation. Editors are projected; no multi-type edit switch remains. Route parameter changes cancel stale reads. Sprite definition metadata and existing binary sprite workspace have distinct boundaries; binary workflow is preserved.

List owns query/paginator form state and injects independent type stores. Numeric queries and result links navigate to detail routes. Archive counts have a separate resource-backed store. Do not add providers at application root.

This approved change supersedes the earlier portal proposal requirement to retain the monolithic CacheTypesStore.

## Final implementation decisions
The shared definition feature owns one private rxMethod write pipeline, exposed through type-specific named commands. Editors use linkedSignal form models with stable initial field shapes and reset from the resource value, without an effect copying read results into another store. The existing sprite image workspace uses rxMethod/tapResponse pipelines so it preserves the prior preview on failed replacement reads and cancels pending work on destruction. No cache store contains async/await or firstValueFrom request wrappers.

## Audit limits
Fallow's duplication findings were resolved by centralizing request lifecycle ownership. Its four newly reported unused resource factory members are false positives: production read features call definitionResource/rangeResource/searchResource, and CacheArchivesStore calls archiveSizesResource. Real route/store tests and fixture runtime requests exercise those same service methods. Do not delete them or add suppression solely to pass this finding. Four unchanged mutation service members have the same inherited warning.

## Automatic list loading
Load the selected archive after sizes arrive and on category changes using the current paginator page size. Category changes clear the submitted query and reset the page. Reuse existing range resources; ID-only types retain lookup guidance. No API or store ownership changes.

## Type-specific API services
Each of the 13 type stores injects its dedicated service through the existing resource features. Services own fixed type endpoints and only supported range/search/write operations. Archive sizes have a separate service. Reuse the existing sprite service for metadata and binary operations. Preserve URLs, payloads, resource cancellation and page-scoped state. No new endpoints or speculative DTO fields. Acceptance: all type reads and supported writes retain their tested contracts; remove the obsolete monolithic service.

Concrete service calls are explicit in each store via resource callbacks. The shared features receive resource constructors instead of service class tokens, keeping dependency injection and type-specific API calls visible at the store boundary.

## Integrated sprite images
Reuse the existing sprite tools component inside the sprite detail page with a fixed ID input. Auto-load frame zero; permit frame preview and confirmed PNG replacement against existing GET/POST image endpoints. Hide standalone lookup/search controls in embedded mode. Refresh metadata and preview after successful replacement. Preserve blob URL cleanup, offline error feedback, existing standalone route and authorization. No backend changes or fake production responses. Acceptance: detail requests its own sprite image, allows retry after failure, targets confirmed upload at that ID, refreshes after save, and releases previews on navigation.
