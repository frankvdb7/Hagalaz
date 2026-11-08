# Texture endpoint DTOs
## Why
Texture material/HDR routes expose decoder records and admin HTTP calls use inline/rendering types.
## What Changes
Dedicated GetTextureMaterialDto and GetTextureHdrDto contracts, explicit decoder mapping and matching admin DTOs. Keep PNG responses and texture headers binary/unchanged.
## Acceptance criteria
1. Material/HDR JSON uses dedicated DTOs with every existing field and no decoder types.
2. Admin requests use matching local DTOs while renderer types remain independent.
3. Material values, HDR intensities, PNG pixels/headers and error behavior remain compatible; backend/admin checks pass.
## Non-goals
No texture decoding/rendering algorithm changes, route changes, JSON wrappers for PNG, new dependencies or authorization changes.
## Capabilities
### New Capabilities
- `texture-contracts`: explicit texture material/HDR REST DTOs.
### Modified Capabilities
None.
## Impact
Texture endpoint/DTO mappings, admin texture HTTP types and focused regression assertions.
