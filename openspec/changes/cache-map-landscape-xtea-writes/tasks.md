# Tasks

## 1. Encrypted write path

- [x] 1.1 Add `XteaEncryptor` and verify with `XteaEncryptorTests` that it inverts `XteaDecryptor`, leaves the header and a short tail plain, and requires four keys.
- [x] 1.2 Add keyed `ICacheWriter.Write` and `ICacheAPI.TryReplaceContainer`. Verify in `CacheWriterIntegrationTests` that the stored bytes are encrypted, reopen with the keys, fail without or with wrong keys, match the stored CRC, advance the version, that wrong keys write nothing, and that all-zero or short keys throw before any read or write.

## 2. Landscape saves

- [x] 2.1 Add `xteaKeys` to `ReplaceLandscape`, the DTO and the endpoint, refusing uncompressed protected files. Verify in `RegionMapArchiveTests` and `MapRenderEndpointTests`: keyed save, missing keys, wrong keys, uncompressed file, unusable keys.
- [x] 2.2 Add the 65535 huge-smart boundary case. Verify `WriteAndReadHugeSmart`.

## 3. Verification

- [x] 3.1 `dotnet build Hagalaz.sln` and the `Hagalaz.Cache.Tests`, `Hagalaz.Cache.Extensions.Tests` and `Hagalaz.Services.Cache.Tests` suites pass.
- [ ] 3.2 Archive this change after merge.
