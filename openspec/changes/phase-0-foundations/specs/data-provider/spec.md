# Spec Delta

## Purpose

Allows TenancyManager to run on JSON seed files (for local development, demos and tests) or on
PostgreSQL (for real environments), with identical observable behaviour.

## ADDED Requirements

### Requirement: DAT-001 Configurable data provider
The system SHALL store and read data using the provider named in configuration, either "Json" or "Postgres", and SHALL refuse to start with any other value.

#### Scenario: Unknown provider is rejected
- **GIVEN** the data provider is configured as "Oracle"
- **WHEN** the application starts
- **THEN** start-up fails with an error listing the supported providers "Json" and "Postgres"

### Requirement: DAT-002 Identical seed data across providers
The system SHALL load the same seed data into either provider, so that the same query returns the same records on JSON and on an empty, newly seeded PostgreSQL database.

#### Scenario: Seeded property list matches
- **GIVEN** the seed data for "Northgate Lettings"
- **WHEN** the property list is read from the "Json" provider and from a newly seeded "Postgres" provider
- **THEN** both return the same properties in the same order
