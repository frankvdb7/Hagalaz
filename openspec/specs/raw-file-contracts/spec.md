# raw-file-contracts Specification

## Purpose
Define the route DTOs for raw file downloads while keeping their responses binary.

## Requirements

### Requirement: Raw downloads use route DTOs and remain binary
Raw file and sub-file endpoints SHALL bind route IDs through dedicated DTOs while retaining binary response bytes, content type and filenames.
#### Scenario: Download raw data
- **WHEN** an existing file or sub-file is requested
- **THEN** route binding resolves the same IDs and returns the same octet-stream payload and download filename
#### Scenario: Missing file
- **WHEN** the requested data does not exist
- **THEN** existing missing-asset behavior remains unchanged
