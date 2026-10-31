## ADDED Requirements
### Requirement: Aspire web app URLs use stable local ports
Aspire SHALL advertise the main web application on HTTP port 4400 and the admin on HTTP port 4410, while retaining proxying and allocated internal listener ports.
#### Scenario: Build local application model
- **WHEN** the AppHost model is configured
- **THEN** the two web resources have distinct fixed public ports and no fixed internal target ports
