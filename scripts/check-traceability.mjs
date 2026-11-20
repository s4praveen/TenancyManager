#!/usr/bin/env node
// Fails if any OpenSpec requirement has no ID, or no test referencing its ID.
//   Requirements: "### Requirement: TEN-003 Deposit cap" in openspec/specs/** and active openspec/changes/**
//   Tests:        Reqnroll tag "@TEN-003", xUnit [Trait("Requirement", "TEN-003")], Playwright title "@TEN-003"
// Usage: node scripts/check-traceability.mjs [--verbose]
import { readdirSync, readFileSync, statSync } from 'node:fs'
import { join, relative } from 'node:path'

const root = new URL('..', import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, '$1')
const verbose = process.argv.includes('--verbose')
const SKIP_DIRS = new Set(['node_modules', 'bin', 'obj', 'archive', '.git', 'test-results', 'playwright-report'])

function* walk(dir) {
  for (const name of readdirSync(dir)) {
    if (SKIP_DIRS.has(name)) continue
    const full = join(dir, name)
    if (statSync(full).isDirectory()) yield* walk(full)
    else yield full
  }
}

const ID = '[A-Z]{3}-\\d{3}'
const requirementRe = /^###\s+Requirement:\s*(.*)$/gm
const idRe = new RegExp(`^(${ID})\\b`)
const testRefRes = [
  new RegExp(`@(${ID})\\b`, 'g'), // .feature tags, Playwright titles
  new RegExp(`Trait\\(\\s*"Requirement"\\s*,\\s*"(${ID})"\\s*\\)`, 'g'), // xUnit
]

const requirements = new Map() // id -> [files]
const missingIds = []

for (const file of walk(join(root, 'openspec'))) {
  if (!file.endsWith('spec.md')) continue
  for (const [, title] of readFileSync(file, 'utf8').matchAll(requirementRe)) {
    const match = title.match(idRe)
    if (!match) {
      missingIds.push(`${relative(root, file)}: "${title.trim()}"`)
      continue
    }
    requirements.set(match[1], [...(requirements.get(match[1]) ?? []), relative(root, file)])
  }
}

const tested = new Map() // id -> [files]
for (const file of walk(join(root, 'tests'))) {
  if (!/\.(feature|cs|ts)$/.test(file) || file.endsWith('.feature.cs')) continue
  const text = readFileSync(file, 'utf8')
  for (const re of testRefRes) {
    for (const [, id] of text.matchAll(re)) tested.set(id, [...new Set([...(tested.get(id) ?? []), relative(root, file)])])
  }
}

const untested = [...requirements.keys()].filter((id) => !tested.has(id)).sort()
const unknown = [...tested.keys()].filter((id) => !requirements.has(id)).sort()

if (verbose) {
  for (const id of [...requirements.keys()].sort()) console.log(`${id}  ->  ${(tested.get(id) ?? ['(none)']).join(', ')}`)
}

let failed = false
if (missingIds.length) {
  failed = true
  console.error(`Requirements without an ID (expected "### Requirement: ABC-123 Name"):\n  ${missingIds.join('\n  ')}`)
}
if (untested.length) {
  failed = true
  console.error(`Requirements with no test referencing them:\n  ${untested.join('\n  ')}`)
}
if (unknown.length) {
  // A warning, not a failure: removed requirements may leave stale tags during a change.
  console.warn(`Warning: tests reference unknown requirement IDs:\n  ${unknown.join('\n  ')}`)
}

if (failed) process.exit(1)
console.log(`Traceability OK: ${requirements.size} requirements, all referenced by tests.`)
