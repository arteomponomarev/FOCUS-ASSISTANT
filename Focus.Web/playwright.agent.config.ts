import { defineConfig } from '@playwright/test'
import config from './playwright.config'

export default defineConfig({
  ...config,
  testIgnore: [],
  testMatch: '**/agent.spec.ts',
  outputDir: 'test-results/agent',
  reporter: [['list'], ['json', { outputFile: 'test-results/agent-results.json' }]],
})
