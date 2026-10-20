## Context

The cache container format stores the BZip2 block beginning at the block magic
`1AY` rather than storing the standalone stream prefix `BZh1`. SharpZipLib
expects the standalone prefix.

## Decision

Before constructing `BZip2InputStream`, detect the `BZh` prefix. If it is
missing, prepend `BZh1` to a new buffer. Keep the existing
input unchanged and leave already complete BZip2 streams on the current path.

## Risks

Invalid input still fails in SharpZipLib. The compatibility branch only changes
the bytes supplied to the decoder when the standard prefix is absent.
