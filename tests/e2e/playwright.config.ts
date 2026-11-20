import { defineConfig, devices } from '@playwright/test'

// Starts the API (Json stub + Dev auth) and the Vite dev server, then drives Chromium.
export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [['github'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      command: 'dotnet run --project ../../src/api/TenancyManager.Api --launch-profile http',
      url: 'http://localhost:5080/health',
      reuseExistingServer: !process.env.CI,
      timeout: 180_000,
      env: { DataProvider__Type: 'Json', Auth__Mode: 'Dev' },
    },
    {
      command: 'npm run dev --prefix ../../src/web',
      url: 'http://localhost:5173',
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
      env: { VITE_AUTH_MODE: 'dev' },
    },
  ],
})
