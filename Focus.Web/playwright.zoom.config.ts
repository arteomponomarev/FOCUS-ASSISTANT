import { defineConfig } from '@playwright/test'
import config from './playwright.config'

export default defineConfig({
  ...config,
  testIgnore: [],
  testMatch: '**/zoom.spec.ts',
  outputDir: 'test-results/zoom',
  reporter: [['list'], ['json', { outputFile: 'test-results/zoom-results.json' }]],
})
