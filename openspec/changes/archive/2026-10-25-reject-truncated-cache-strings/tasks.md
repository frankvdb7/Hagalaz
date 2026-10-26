## 1. Reject truncated strings

- [x] 1.1 Make `ReadString` distinguish EOF from the string terminator and throw `EndOfStreamException`.
- [x] 1.2 Verify the failure through `ObjectTypeCodec.Decode` and preserve terminated-string behavior.
