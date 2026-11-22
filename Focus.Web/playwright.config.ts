import { defineConfig, devices } from '@playwright/test'

const browserPath = process.env.FOCUS_BROWSER_PATH
export default defineConfig({
  testDir: './e2e',
  testIgnore: ['**/zoom.spec.ts', '**/qa.spec.ts', '**/agent.spec.ts'],
  fullyParallel: false,
  workers: 1,
  timeout: 45_000,
  reporter: [['list'], ['json', { outputFile: 'test-results/results.json' }]],
  use: {
    baseURL: 'http://127.0.0.1:4175',
    ...devices['Desktop Chrome'],
    locale: 'en-US',
    launchOptions: browserPath ? { executablePath: browserPath } : undefined,
    trace: 'off',
    screenshot: 'off',
    video: 'off',
  },
  webServer: [
    {
      command: 'node scripts/e2e-api.mjs',
      url: 'http://127.0.0.1:5188/health/ready',
      timeout: 90_000,
      reuseExistingServer: false,
    },
    {
      command: 'npm run preview -- --strictPort --port 4175',
      url: 'http://127.0.0.1:4175/login',
      env: { FOCUS_API_ORIGIN: 'http://127.0.0.1:5188' },
      timeout: 30_000,
      reuseExistingServer: false,
    },
  ],
})
