# Spec Delta

## Purpose

Lets hosting platforms and operators check, without signing in, that the API is running and which
data provider it uses.

## ADDED Requirements

### Requirement: SYS-001 Health endpoint
The system SHALL expose `GET /health` without authentication. It SHALL return HTTP 200 with status "Healthy" and the active data provider name when the API and its data store are reachable.

#### Scenario: Healthy on the JSON provider
- **GIVEN** the API is running with the "Json" data provider
- **WHEN** an anonymous caller requests the health endpoint
- **THEN** the response status is 200
- **AND** the health status is "Healthy"
- **AND** the data provider is "Json"
