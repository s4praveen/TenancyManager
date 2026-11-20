Feature: Agency scoping
  Each agency sees only its own records.
  Spec: openspec/specs/agency-scoping

  @AGY-001
  Scenario: Another agency's properties are excluded
    Given the current agency is "Northgate Lettings"
    And the store also holds properties for "Southbank Homes"
    When an agent requests the property list
    Then every property returned belongs to "Northgate Lettings"
    And no "Southbank Homes" property is returned

  @AGY-002
  Scenario: Requesting a Southbank property by id
    Given the current agency is "Northgate Lettings"
    When an agent requests a property that belongs to "Southbank Homes" by its id
    Then the response status is 404
