# Design

`BigEndianMemoryStreamExtensions.ReadString` owns null-terminated string parsing. Read one byte at a time with `MemoryStream.ReadByte`, which distinguishes EOF (`-1`) from the valid `0xFF` byte. Throw `EndOfStreamException` at EOF and preserve the existing ASCII character mapping and zero-byte terminator.

The regression passes an unterminated object name through `ObjectTypeCodec.Decode`, proving the shared reader fails on a reachable definition-codec path. No cache, provider, or endpoint behavior needs a new abstraction.
