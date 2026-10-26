# Design

`CreateChecksumTable` will enter the existing `CacheApi` read lock before reading any reference-table files and release it in `finally` after the full table is built. The write lock already spans local archive and reference-table persistence, so the two operations become mutually exclusive within one cache instance. The file-store and checksum format remain unchanged.
