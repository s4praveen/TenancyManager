# Spec Delta

## Purpose

Controls who can use TenancyManager: users must be authenticated, hold an agency role, and the
local Dev sign-in can never be used in production.

## ADDED Requirements

### Requirement: IAM-001 Authentication required
The system SHALL reject any request to an `/api` endpoint that carries no valid credentials with HTTP 401.

#### Scenario: Anonymous request is rejected
- **GIVEN** a caller with no credentials
- **WHEN** the caller requests the property list
- **THEN** the response status is 401

### Requirement: IAM-002 Agency role required
The system SHALL allow `/api` access only to users holding at least one agency role (`AgencyAdmin`, `Agent`, `Accounts`, `ReadOnly`), and SHALL reject an authenticated user with no agency role with HTTP 403.

#### Scenario: User without an agency role is forbidden
- **GIVEN** an authenticated user "nobody@example.test" with no roles
- **WHEN** the user requests the property list
- **THEN** the response status is 403

#### Scenario: User with an agency role is allowed
- **GIVEN** an authenticated user "agent@example.test" with role "Agent"
- **WHEN** the user requests the property list
- **THEN** the response status is 200

### Requirement: IAM-003 Current user
The system SHALL return the signed-in user's display name, roles and current agency from `GET /api/me`.

#### Scenario: Agent views their profile
- **GIVEN** an authenticated user "agent@example.test" with role "Agent"
- **WHEN** the user requests their profile
- **THEN** the response contains user "agent@example.test"
- **AND** the roles include "Agent"
- **AND** the agency name is "Northgate Lettings"

### Requirement: IAM-004 Dev sign-in disabled in production
The system SHALL refuse to start when Dev authentication is configured and the hosting environment is Production.

#### Scenario: Production with Dev auth fails fast
- **GIVEN** the environment is "Production"
- **AND** the authentication mode is "Dev"
- **WHEN** the application starts
- **THEN** start-up fails with an error explaining that Dev authentication is not allowed in Production
