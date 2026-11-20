Feature: Properties
  Agents can see the properties their agency manages and which UK jurisdiction applies.
  Spec: openspec/specs/properties

  Background:
    Given an authenticated user "agent@example.test" with role "Agent"

  @PRP-001
  Scenario: Agent lists Northgate properties
    When the user requests the property list
    Then 4 properties are returned
    And the first property is "14 Castle Street, Cardiff" in jurisdiction "Wales" owned by "Gwen Davies"

  @PRP-002
  Scenario: Seeded properties cover all three jurisdictions
    When the user requests the property list
    Then the jurisdictions returned are exactly "England", "Scotland" and "Wales"

  @PRP-003
  Scenario: Unknown property id
    When the user requests a property with an id that does not exist
    Then the response status is 404
