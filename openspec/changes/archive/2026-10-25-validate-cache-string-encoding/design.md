# Design

Cache strings are read as one character per byte and terminated by byte zero. The writer will enforce the inverse domain: every character must be non-null and fit in one Latin-1 byte. Object-name validation returns HTTP 400 before definition lookup or persistence, while the shared writer also rejects invalid values from other callers.

The object mutation integration test will use high-byte characters in both the replacement name and an existing action string, then read the persisted definition back through the Cache service.
