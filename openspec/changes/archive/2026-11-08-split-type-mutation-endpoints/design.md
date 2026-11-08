# Design
Use six endpoint extension classes in the existing Types directory, mapped directly from the existing protected types route group. Move handlers without changing their bodies. TypeMutationUtilities retains image reading and gains the existing shared decode/validate/persist methods. Keep one common TypeMutationResponse record; type-specific records move with their endpoint owner. Remove the old aggregate class, which has no external consumer beyond in-repo mappings/tests. JSON records use unchanged properties. Existing 73 service tests are the baseline; run them using isolated artifacts to avoid the live service DLL lock.

## Verification
All 73 cache-service tests passed, covering the existing type mutation HTTP routes, validation, persistence coherence, item duplication and sprite replacement. A separate cache-service build passed with zero errors and no source compiler warnings; existing NuGet vulnerability/pruning warnings remain. git diff --check and strict OpenSpec validation passed. No browser checks or live cache mutations were performed. Existing service was not restarted.

## Definition ownership refinement
The approved organize-definition-endpoints follow-up merges each type's mutation class with its read handlers into ItemDefinitionEndpoints, NpcDefinitionsEndpoints and the corresponding type owner. The separate mutation endpoint classes are removed; TypeEndpoints is only registration composition. Existing shared persistence remains unchanged.
