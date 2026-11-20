# Spec Delta

## Purpose

Keeps each letting agency's data separate, so that the single-agency product can later become a
multi-agency SaaS without data leaking between agencies.

## ADDED Requirements

### Requirement: AGY-001 Lists show only the current agency's records
The system SHALL return only records belonging to the current agency in any list.

#### Scenario: Another agency's properties are excluded
- **GIVEN** the current agency is "Northgate Lettings"
- **AND** the store also holds properties for "Southbank Homes"
- **WHEN** an agent requests the property list
- **THEN** every property returned belongs to "Northgate Lettings"
- **AND** no "Southbank Homes" property is returned

### Requirement: AGY-002 Another agency's record is not found
The system SHALL respond with HTTP 404 when a user requests, by identifier, a record that belongs to a different agency, so the record's existence is not revealed.

#### Scenario: Requesting a Southbank property by id
- **GIVEN** the current agency is "Northgate Lettings"
- **WHEN** an agent requests a property that belongs to "Southbank Homes" by its id
- **THEN** the response status is 404
