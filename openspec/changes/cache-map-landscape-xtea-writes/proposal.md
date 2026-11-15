# Cache map landscape XTEA writes

## Why

Landscape saves refuse any region whose `lX_Y` file is XTEA-protected, because the write path reads and writes without keys. Protected regions could not be edited, and an unprotected write path must never be mistaken for a safe one (issue #556).

## What Changes

- Add `XteaEncryptor`, the exact inverse of `XteaDecryptor`.
- `ICacheWriter.Write` and `ICacheAPI.TryReplaceContainer` gain overloads taking four XTEA keys: the file is encoded, encrypted after its 5-byte header, and its reference-table version, CRC and digest cover the encrypted bytes as stored. All-zero or wrong-length keys are rejected before anything is read or written.
- `IRegionMapArchive.ReplaceLandscape` and `POST /maps/{id}/landscape` accept optional `xteaKeys`. Keys are never guessed, defaulted, or stored; without correct keys a protected file does not decode and nothing is written.
- An uncompressed protected file is refused, because it has no integrity check that proves the keys right.
- Scope ends at landscape (`lX_Y`). Terrain, underwater files, instances and a transaction framework are out of scope.

## Capabilities

### Modified Capabilities

- `cache-map-landscape-mutations` replaces the "encrypted landscapes are refused" rule with explicit-key writes.

## Impact

`Hagalaz.Cache` writer and `CacheApi`, `RegionMapArchive`, the Cache service landscape endpoint and DTO. Existing callers without keys behave as before.
