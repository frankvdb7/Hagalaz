## Why

The real revision-742 client reaches GameUpdate but remains at 0% because the
RuneScape cache stores BZip2 payloads without the leading `BZh` stream marker.
The shared cache decoder passes those payloads directly to SharpZipLib, which
rejects them before the checksum table can be served.

## What changes

- Accept cache-style BZip2 payloads with the omitted `BZh1` marker.
- Preserve support for ordinary BZip2 streams that already contain the marker.
- Add a focused regression test using the existing compressor with its marker
  removed.

## Impact

This affects only `Hagalaz.Cache.Utilities.CompressionUtilities`, which is used
by GameUpdate and cache readers. No cache files, client code, protocol, or
reconnect behavior changes.

## Acceptance criteria

- Cache-style BZip2 payloads decode successfully.
- Full BZip2 streams continue to decode successfully.
- The focused cache test passes and GameUpdate can serve the real cache
  checksum table to the revision-742 client.

## Non-goals

- Do not replace or regenerate the local cache.
- Do not change GameUpdate routing or client update behavior.
- Do not change the reconnect implementation.
