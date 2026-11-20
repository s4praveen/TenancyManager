# Seed data

Fictional data only: names use the `example.test` domain and addresses are illustrative.

One folder per agency: `agencies/<slug>/{agency,landlords,properties}.json`.
- The **Json** data provider serves these files directly (in memory; edits restart with the API).
- The **Postgres** provider loads the same files into an empty database on start-up.

`Southbank Homes` exists only to prove agency isolation (AGY-001/002). The default agency is
`Northgate Lettings` (`11111111-1111-1111-1111-111111111111`).
Acceptance tests depend on these records, so update the feature files when you change them.
