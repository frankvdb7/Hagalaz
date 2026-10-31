# Design
Extract GetArchiveDto(FileId, Entries) and ArchiveEntryDto(SubFileId, DataBase64, DataLength) into Features/Archives/Dtos with its matching namespace. Retain existing physical member-index semantics and disposal scopes. No admin consumer uses this archive read route. Verify existing JSON using actual cache decoding over isolated file-store fixtures; no production mutation is needed.

## Verification
The service and test project built successfully through dotnet test; all 82 service tests passed. ReadArchive_RetainsMemberOrderBase64AndEmptyEntries covers single and multiple members, exact IDs/base64/lengths and an empty member. ReadMissingArchive_RetainsNotFoundResponse covers the existing missing-archive guard. Strict OpenSpec validation and git diff --check passed, with an existing Git line-ending notice. No new source compiler warnings; pre-existing NuGet warnings remain. Verification used read-only isolated stores, with no live service restart or cache mutation.
