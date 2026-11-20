# Spec Delta

## Purpose

Gives agents a register of the properties their agency manages, including the landlord and the
UK jurisdiction (England, Wales or Scotland) whose law applies to each one.

## ADDED Requirements

### Requirement: PRP-001 List properties
The system SHALL return the current agency's properties ordered by town and then first address line. Each item SHALL include its id, address, postcode, jurisdiction and landlord display name.

#### Scenario: Agent lists Northgate properties
- **GIVEN** an authenticated user "agent@example.test" with role "Agent"
- **WHEN** the user requests the property list
- **THEN** 4 properties are returned
- **AND** the first property is "14 Castle Street, Cardiff" in jurisdiction "Wales" owned by "Gwen Davies"

### Requirement: PRP-002 Property jurisdiction
The system SHALL record exactly one jurisdiction for every property, one of "England", "Wales" or "Scotland".

#### Scenario: Seeded properties cover all three jurisdictions
- **GIVEN** an authenticated user "agent@example.test" with role "Agent"
- **WHEN** the user requests the property list
- **THEN** the jurisdictions returned are exactly "England", "Scotland" and "Wales"

### Requirement: PRP-003 Property detail
The system SHALL return a single property of the current agency by id, and SHALL respond with HTTP 404 for an unknown id.

#### Scenario: Unknown property id
- **GIVEN** an authenticated user "agent@example.test" with role "Agent"
- **WHEN** the user requests a property with an id that does not exist
- **THEN** the response status is 404
