import { defineConfig } from '@playwright/test'
import config from './playwright.config'

export default defineConfig({
  ...config,
  testIgnore: [],
  testMatch: '**/qa.spec.ts',
  outputDir: 'test-results/qa',
  reporter: [['list'], ['json', { outputFile: 'test-results/qa-results.json' }]],
})
