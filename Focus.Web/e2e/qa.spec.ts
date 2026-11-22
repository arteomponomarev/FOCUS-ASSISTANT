import { expect, test } from '@playwright/test'
import type { Page } from '@playwright/test'
import { nav, registerByApi, signIn } from './support'

async function holdRequests(page: Page, pattern: string) {
  let release = () => {}
  const gate = new Promise<void>((resolve) => {
    release = resolve
  })
  await page.route(
    pattern,
    async (route) => {
      await gate
      await route.continue()
    },
    { times: 1 },
  )
  return release
}

test('updates a cached overview after completion and browses multiple history pages', async ({
  page,
  request,
}) => {
  const account = await registerByApi(request, 'QA Researcher')
  await page.goto('/login')
  const login = await signIn(page, account)
  const { accessToken }: { accessToken: string } = await login.json()
  const headers = { Authorization: 'Bearer ' + accessToken }
  await expect(page.getByRole('heading', { name: 'Your first session starts here' })).toBeVisible()

  await page.getByRole('link', { name: 'New session', exact: true }).click()
  await page.getByRole('button', { name: 'Add goal', exact: true }).click()
  await page.getByRole('textbox', { name: 'Goal 1', exact: true }).fill('QA cache verification')
  await page.getByRole('button', { name: 'Create session' }).click()
  await expect(page.getByRole('heading', { name: 'QA cache verification' })).toBeVisible()
  await nav(page, 'Overview').click()
  await expect(page.getByText('Sessions completed', { exact: true }).locator('..')).toContainText(
    '0',
  )
  await nav(page, 'Sessions').click()
  await page.getByRole('link', { name: 'QA cache verification', exact: true }).click()
  await page.getByRole('button', { name: 'Start session' }).click()
  await page.getByRole('button', { name: 'Finish session' }).click()
  const dialog = page.getByRole('dialog')
  await dialog.getByLabel('Concentration').selectOption('4')
  await dialog.getByLabel('Productivity').selectOption('3')
  await dialog.getByLabel('Outcome for QA cache verification').selectOption('Completed')
  await dialog.getByRole('button', { name: 'Save completion' }).click()
  await expect(page.getByRole('heading', { name: 'Completed session' })).toBeVisible()

  const allowSummary = await holdRequests(page, '**/api/v1/analytics/summary?*')
  try {
    await nav(page, 'Overview').click()
    await expect(page.getByText('Loading overview…')).toBeAttached()
    await expect(page.getByText('Sessions completed', { exact: true })).toHaveCount(0)
    await expect(page.getByRole('heading', { name: 'Session activity' })).toHaveCount(0)
  } finally {
    allowSummary()
  }
  await expect(page.getByText('Sessions completed', { exact: true }).locator('..')).toContainText(
    '1',
  )
  await expect(
    page.getByText('Self-reported concentration', { exact: true }).first().locator('..'),
  ).toContainText('4 / 5')
  const changedRange = page.waitForResponse(
    (response) => new URL(response.url()).pathname === '/api/v1/analytics/summary',
  )
  await page.getByLabel('Date range').selectOption('30')
  const range = new URL((await changedRange).url()).searchParams
  expect(Date.parse(range.get('To') ?? '') - Date.parse(range.get('From') ?? '')).toBeGreaterThan(
    29 * 86_400_000,
  )

  for (let index = 1; index <= 11; index++) {
    expect(
      (
        await request.post('/api/v1/sessions', {
          headers,
          data: {
            plannedDurationSeconds: 1500,
            goals: ['QA fixture ' + index],
          },
        })
      ).status(),
    ).toBe(201)
  }
  await nav(page, 'Sessions').click()
  const rows = page.getByRole('table').getByRole('row')
  await expect(rows).toHaveCount(11)
  await page.getByRole('button', { name: 'Next', exact: true }).click()
  await expect(page.getByText('Page 2 of 2')).toBeVisible()
  await expect(rows).toHaveCount(3)
  await page.getByLabel('Status', { exact: true }).selectOption('Completed')
  await expect(rows).toHaveCount(2)
  await page.getByRole('link', { name: 'QA cache verification', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Completed session' })).toBeVisible()
})

test('handles invalid login, rotates credentials, and signs out on revocation', async ({
  page,
  request,
}) => {
  const account = await registerByApi(request, 'QA Authentication')
  await page.goto('/login')
  await page.getByLabel('Email', { exact: true }).fill(account.email)
  await page.getByLabel('Password', { exact: true }).fill('invalid-test-password')
  const invalid = page.waitForResponse(
    (reply) => new URL(reply.url()).pathname === '/api/v1/auth/login',
  )
  await page.getByRole('button', { name: 'Sign in', exact: true }).click()
  expect((await invalid).status()).toBe(401)
  await expect(page.getByRole('alert')).toContainText('check your credentials')
  const initial = await signIn(page, account)
  const original: { accessToken: string; refreshToken: string; expiresIn: number } =
    await initial.json()
  await expect(page.getByRole('heading', { name: 'Overview', exact: true })).toBeVisible()

  await page.clock.setFixedTime(Date.now() + (original.expiresIn + 1) * 1000)
  const renewal = page.waitForResponse(
    (reply) => new URL(reply.url()).pathname === '/api/v1/auth/refresh',
  )
  await nav(page, 'Sessions').click()
  const rotated = await renewal
  expect(rotated.status()).toBe(200)
  const current: { accessToken: string; refreshToken: string } = await rotated.json()
  expect(current.accessToken === original.accessToken).toBe(false)
  expect(current.refreshToken === original.refreshToken).toBe(false)
  await expect(page.getByRole('heading', { name: 'No matching sessions' })).toBeVisible()

  const revoked = await request.post('/api/v1/auth/logout', {
    headers: { Authorization: 'Bearer ' + current.accessToken },
  })
  expect(revoked.status()).toBe(204)
  await nav(page, 'Analytics').click()
  await expect(page).toHaveURL(/\/login$/)
  expect(
    await page.evaluate(() => ({
      keys: Object.keys(localStorage),
      session: sessionStorage.length,
    })),
  ).toEqual({ keys: ['focus-assistant-theme'], session: 0 })
})

test('updates the profile and password and recovers from a disconnected history request', async ({
  page,
  request,
  context,
}) => {
  const account = await registerByApi(request, 'QA Account')
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/login')
  await signIn(page, account)
  await nav(page, 'Sessions').click()
  await expect(page.getByRole('heading', { name: 'No matching sessions' })).toBeVisible()
  await context.setOffline(true)
  try {
    await page.getByLabel('Status', { exact: true }).selectOption('Completed')
    await expect(page.getByRole('alert')).toContainText('Could not reach the server')
  } finally {
    await context.setOffline(false)
  }
  await page.getByRole('button', { name: 'Try again' }).click()
  await expect(page.getByRole('heading', { name: 'No matching sessions' })).toBeVisible()

  const allowReport = await holdRequests(page, '**/api/v1/analytics/statistics?*')
  try {
    await nav(page, 'Analytics').click()
    await expect(page.getByText('Loading report…')).toBeAttached()
    await expect(page.getByRole('heading', { name: 'Period totals' })).toHaveCount(0)
  } finally {
    allowReport()
  }
  await expect(page.getByRole('heading', { name: 'Analytics', exact: true })).toBeInViewport()
  await page.getByRole('heading', { name: 'Period totals' }).scrollIntoViewIfNeeded()
  await expect.poll(() => page.evaluate(() => window.scrollY)).toBeGreaterThan(100)
  await nav(page, 'Settings').click()
  await expect(page.getByRole('heading', { name: 'Settings', exact: true })).toBeInViewport()
  await page.getByLabel('Display name').fill('QA Renamed')
  await page.getByRole('button', { name: 'Save display name' }).click()
  await expect(page.getByRole('status')).toContainText('Changes saved')

  const nextPassword = account.password + 'Updated'
  const current = page.getByLabel('Current password', { exact: true })
  await current.fill('invalid-current-password')
  await page.getByLabel('New password', { exact: true }).fill(nextPassword)
  await page.getByLabel('Confirm new password', { exact: true }).fill(nextPassword)
  await page.getByRole('button', { name: 'Update password' }).click()
  await expect(page.getByRole('alert')).toContainText('Check your current password')
  await expect(page).toHaveURL(/\/settings$/)
  await current.fill(account.password)
  await page.getByRole('button', { name: 'Update password' }).click()
  await expect(page).toHaveURL(/\/login$/)
  await signIn(page, { ...account, password: nextPassword })
  await nav(page, 'Overview').click()
  await expect(page.getByRole('heading', { name: 'Welcome, QA Renamed' })).toBeVisible()
})
