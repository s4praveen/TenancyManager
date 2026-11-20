import { expect, test } from '@playwright/test'

// Requirement tags (@PRP-001 etc.) are picked up by scripts/check-traceability.mjs.

test('@PRP-001 @IAM-003 agent signs in and sees the Northgate property register', async ({ page }) => {
  await page.goto('/')

  await page.getByLabel('User').fill('agent@example.test')
  await page.getByRole('button', { name: 'Sign in' }).click()

  await expect(page.getByTestId('current-user')).toContainText('Northgate Lettings')

  const table = page.getByRole('table', { name: 'Properties' })
  await expect(table.getByRole('row')).toHaveCount(5) // header + 4 properties
  await expect(table.getByRole('row').nth(1)).toContainText('14 Castle Street, Cardiff')
  await expect(table.getByRole('row').nth(1)).toContainText('Wales')
  await expect(table).not.toContainText('Southwark') // AGY-001: other agency hidden
})
