Feature: Identity and access
  Only authenticated users holding an agency role can use the API,
  and Dev sign-in can never run in Production.
  Spec: openspec/specs/identity-access

  @IAM-001
  Scenario: Anonymous request is rejected
    Given a caller with no credentials
    When the caller requests the property list
    Then the response status is 401

  @IAM-002
  Scenario: User without an agency role is forbidden
    Given an authenticated user "nobody@example.test" with no roles
    When the user requests the property list
    Then the response status is 403

  @IAM-002
  Scenario: User with an agency role is allowed
    Given an authenticated user "agent@example.test" with role "Agent"
    When the user requests the property list
    Then the response status is 200

  @IAM-003
  Scenario: Agent views their profile
    Given an authenticated user "agent@example.test" with role "Agent"
    When the user requests their profile
    Then the response contains user "agent@example.test"
    And the roles include "Agent"
    And the agency name is "Northgate Lettings"

  @IAM-004
  Scenario: Production with Dev auth fails fast
    Given the environment is "Production"
    And the authentication mode is "Dev"
    When the application starts
    Then start-up fails with an error containing "Dev authentication is not allowed in Production"
