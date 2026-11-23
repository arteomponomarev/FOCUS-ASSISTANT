import { chromium, expect, test } from '@playwright/test'
import { nav, registerByApi, signIn } from './support'

test('retains the main experience at native browser zoom 125%, 150% and 200%', async ({
  request,
}) => {
  const account = await registerByApi(request, 'Zoom Tester')
  const context = await chromium.launchPersistentContext('', {
    channel: 'chromium',
    baseURL: 'http://127.0.0.1:4175',
    viewport: { width: 1440, height: 1000 },
  })
  try {
    context.setDefaultTimeout(8_000)
    context.setDefaultNavigationTimeout(8_000)
    const page = context.pages()[0] ?? (await context.newPage())
    const browserSettings = await context.newPage()
    await browserSettings.goto('chrome://settings/appearance')
    await test.step('Open the app and sign in', async () => {
      await page.bringToFront()
      await page.goto('/login')
      await signIn(page, account)
      await expect(page).toHaveURL(/\/dashboard$/)
      await expect(page.getByRole('heading', { name: 'Overview', exact: true })).toBeVisible()
    })
    const original = await page.evaluate(() => window.devicePixelRatio)
    for (const scale of [1.25, 1.5, 2]) {
      await test.step('Set native browser zoom to ' + scale * 100 + '%', async () => {
        await browserSettings.bringToFront()
        await browserSettings.evaluate((factor) => {
          const browser = globalThis as typeof globalThis & {
            chrome: { settingsPrivate: { setDefaultZoom: (value: number) => void } }
          }
          browser.chrome.settingsPrivate.setDefaultZoom(factor)
        }, scale)
        await page.bringToFront()
        await expect
          .poll(() => page.evaluate(() => window.devicePixelRatio))
          .toBeCloseTo(original * scale)
        await expect
          .poll(() => page.evaluate(() => window.innerWidth))
          .toBe(Math.round(1440 / scale))
      })
      for (const section of ['Overview', 'Sessions', 'Analytics', 'Settings', 'Privacy']) {
        if (section === 'Privacy') {
          await page
            .getByRole('navigation', { name: 'Settings sections' })
            .getByRole('link', { name: 'Privacy' })
            .click()
        } else {
          await nav(page, section).click()
        }
        const heading = section === 'Privacy' ? 'Computer Vision' : section
        await expect(page.getByRole('heading', { name: heading, exact: true })).toBeVisible()
        await expect(page.locator('.skeleton-wrap, .busy')).toHaveCount(0)
        expect(
          await page.evaluate(
            () => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1,
          ),
        ).toBe(true)
      }
    }
  } finally {
    await context.close()
  }
})
