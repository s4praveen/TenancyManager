Feature: System health and data provider
  Spec: openspec/specs/system-health, openspec/specs/data-provider

  @SYS-001
  Scenario: Healthy on the JSON provider
    Given the API is running with the "Json" data provider
    When an anonymous caller requests the health endpoint
    Then the response status is 200
    And the health status is "Healthy"
    And the data provider is "Json"

  @DAT-001
  Scenario: Unknown provider is rejected
    Given the data provider is configured as "Oracle"
    When the application starts
    Then start-up fails with an error containing "Supported providers: Json, Postgres"

  # DAT-002 (identical seed data across providers) needs PostgreSQL, so it is a contract test in
  # TenancyManager.IntegrationTests (PostgresPropertyRepositoryTests, Trait Requirement=DAT-002).
