# container-contracts Specification

## Purpose
Keep Cache API container reads and writes on explicit request and response DTOs.

## Requirements

### Requirement: Container routes use explicit DTOs
Container JSON routes SHALL accept and return dedicated DTOs while preserving numeric compression, base64 data, version information and existing validation.
#### Scenario: Container read or write
- **WHEN** an existing container request is made
- **THEN** its method, URL, JSON shape, defaults and cache behavior remain compatible
