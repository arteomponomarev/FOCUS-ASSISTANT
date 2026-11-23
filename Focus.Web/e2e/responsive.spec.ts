import { mkdir } from 'node:fs/promises'
import { resolve } from 'node:path'
import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'
import type { APIRequestContext, Page } from '@playwright/test'
import { nav, registerByApi, signIn } from './support'

async function seed(
  request: APIRequestContext,
  account: Awaited<ReturnType<typeof registerByApi>>,
) {
  const login = await request.post('/api/v1/auth/login', {
    data: { email: account.email, password: account.password },
  })
  expect(login.ok()).toBe(true)
  const result: { accessToken: string } = await login.json()
  const headers = { Authorization: 'Bearer ' + result.accessToken }
  const consent = await request.patch('/api/v1/settings/privacy', {
    headers,
    data: { telemetryEnabled: true },
  })
  expect(consent.ok()).toBe(true)
  async function newSession(title: string) {
    const response = await request.post('/api/v1/sessions', {
      headers,
      data: { plannedDurationSeconds: 2700, goals: [title] },
    })
    expect(response.status()).toBe(201)
    const data: { id: string; goals: { id: string }[] } = await response.json()
    const route = '/api/v1/sessions/' + data.id
    expect((await request.post(route + '/start', { headers })).ok()).toBe(true)
    return { route, goal: data.goals[0]?.id }
  }
  const completed = await newSession('Review research notes')
  const startedAt = new Date().toISOString()
  await new Promise((resolve) => setTimeout(resolve, 1100))
  const outcome = await request.post(completed.route + '/activity/batch', {
    headers,
    data: {
      items: [
        {
          clientEventId: globalThis.crypto.randomUUID(),
          startedAt,
          endedAt: new Date().toISOString(),
          keyboardEventCount: 16,
          mouseClickCount: 3,
          windowSwitchCount: 1,
          idleSeconds: 0,
          activeProcessName: 'editor',
        },
      ],
    },
  })
  expect(outcome.ok()).toBe(true)
  const complete = await request.post(completed.route + '/complete', {
    headers,
    data: {
      selfReportedConcentration: 4,
      selfReportedProductivity: 3,
      reflection: 'Reviewed the main findings and noted follow-up questions.',
      goalOutcomes: [{ goalId: completed.goal, status: 'Completed' }],
    },
  })
  expect(complete.ok()).toBe(true)
  await newSession('Draft the research methods')
}

async function ready(page: Page, title: string) {
  await expect(page.getByRole('heading', { name: title, exact: true })).toBeVisible()
  await expect(page.locator('.skeleton-wrap, .busy').first()).toHaveCount(0)
}

async function noOverflow(page: Page) {
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1,
    ),
  ).toBe(true)
}

async function touchTargets(page: Page) {
  const undersized = await page
    .locator(
      'button, a:not(.skip), select, textarea, input:not([type="checkbox"]), .toggle-field, summary',
    )
    .evaluateAll((elements) =>
      elements
        .filter((element) => {
          const size = element.getBoundingClientRect()
          return size.width > 0 && size.height > 0 && (size.width < 43.9 || size.height < 43.9)
        })
        .map(
          (element) =>
            element.getAttribute('aria-label') ||
            element.textContent?.trim().slice(0, 60) ||
            element.tagName,
        ),
    )
  expect(undersized).toEqual([])
}

async function auditAxe(page: Page) {
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze()
  expect(
    results.violations.map(({ id, nodes }) => ({ id, targets: nodes.map((node) => node.target) })),
  ).toEqual([])
}

async function captureQa(page: Page, name: string) {
  if (process.env.FOCUS_CAPTURE_QA !== '1') return
  const folder = resolve('../artifacts/frontend-qa/screens')
  await mkdir(folder, { recursive: true })
  await page.screenshot({ path: resolve(folder, name + '.png'), animations: 'disabled' })
}

const layouts = [
  { width: 320, height: 720 },
  { width: 375, height: 812 },
  { width: 390, height: 844 },
  { width: 430, height: 932 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 844, height: 390 },
  { width: 667, height: 375 },
  { width: 1440, height: 1000 },
  { width: 1920, height: 1080 },
  { width: 1152, height: 800 },
  { width: 960, height: 667 },
  { width: 720, height: 500 },
]

test('checks responsive routes, dark theme, touch targets, axe and dialog focus', async ({
  page,
  request,
}) => {
  test.setTimeout(150_000)
  const account = await registerByApi(request, 'Alex Morgan')
  await seed(request, account)
  await page.goto('/login')
  for (const layout of layouts) {
    await page.setViewportSize(layout)
    await ready(page, 'Welcome back')
    await noOverflow(page)
    await page.getByRole('link', { name: 'Create an account' }).click()
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()
    await noOverflow(page)
    await page.getByRole('link', { name: 'Sign in', exact: true }).click()
  }
  await page.setViewportSize({ width: 390, height: 844 })
  await page.goto('/login')
  await auditAxe(page)
  await touchTargets(page)
  if (process.env.FOCUS_CAPTURE_QA === '1') {
    await captureQa(page, 'mobile-light-login')
    await page.getByRole('button', { name: 'Switch to dark theme' }).click()
    await auditAxe(page)
    await captureQa(page, 'mobile-dark-login')
    await page.getByRole('link', { name: 'Create an account' }).click()
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()
    await touchTargets(page)
    await auditAxe(page)
    await captureQa(page, 'mobile-dark-register')
    await page.getByRole('button', { name: 'Switch to light theme' }).click()
    await auditAxe(page)
    await captureQa(page, 'mobile-light-register')
    await page.getByRole('link', { name: 'Sign in', exact: true }).click()
  }
  await signIn(page, account)
  await ready(page, 'Overview')
  for (const layout of layouts) {
    await page.setViewportSize(layout)
    for (const [section, title] of [
      ['Overview', 'Overview'],
      ['Sessions', 'Sessions'],
      ['Analytics', 'Analytics'],
      ['Settings', 'Settings'],
    ]) {
      if (!section || !title) throw new Error('Incomplete route definition')
      await nav(page, section).click()
      await ready(page, title)
      await noOverflow(page)
      if (layout.width === 390) await touchTargets(page)
      if (section === 'Overview') await captureQa(page, 'overview-' + layout.width)
    }
    await page
      .getByRole('navigation', { name: 'Settings sections' })
      .getByRole('link', { name: 'Privacy' })
      .click()
    await expect(page.getByRole('heading', { name: 'Computer Vision' })).toBeVisible()
    const cameraConsent = page.getByRole('checkbox', {
      name: 'Allow coarse camera observation totals',
    })
    await expect(cameraConsent).toBeEnabled()
    await expect(cameraConsent).not.toBeChecked()
    await noOverflow(page)
    if (layout.width === 390) {
      await touchTargets(page)
      await auditAxe(page)
      await captureQa(page, 'mobile-light-privacy')
      await page.getByRole('button', { name: 'Switch to dark theme' }).click()
      await auditAxe(page)
      await captureQa(page, 'mobile-dark-privacy')
      await page.getByRole('button', { name: 'Switch to light theme' }).click()
    }
    await nav(page, 'Overview').click()
    await page.getByRole('link', { name: 'New session', exact: true }).click()
    await ready(page, 'New session')
    await noOverflow(page)
    if (layout.width === 390) await touchTargets(page)
    if (layout.width === 320) await captureQa(page, 'creation-320')
    await nav(page, 'Sessions').click()
    await page.getByRole('link', { name: 'Draft the research methods', exact: true }).click()
    await ready(page, 'Draft the research methods')
    await noOverflow(page)
    if (layout.width === 390) await touchTargets(page)
    if (layout.width === 844) await captureQa(page, 'active-landscape-844')
  }
  await page.setViewportSize({ width: 390, height: 844 })
  await nav(page, 'Overview').click()
  await ready(page, 'Overview')
  await auditAxe(page)
  await page.getByRole('button', { name: 'Switch to dark theme' }).click()
  await expect(page.locator('html')).toHaveAttribute('data-theme', 'dark')
  await auditAxe(page)
  await page.getByRole('button', { name: 'Switch to light theme' }).click()

  await nav(page, 'Sessions').click()
  await page.getByRole('link', { name: 'Draft the research methods', exact: true }).click()
  await ready(page, 'Draft the research methods')
  await noOverflow(page)
  const finish = page.getByRole('button', { name: 'Finish session' })
  await page.keyboard.press('Tab')
  await finish.focus()
  expect(await finish.evaluate((element) => getComputedStyle(element).outlineStyle)).toBe('solid')
  await page.keyboard.press('Enter')
  await expect(page.getByRole('dialog', { name: 'Finish session' })).toBeVisible()
  await auditAxe(page)
  await page.keyboard.press('Tab')
  expect(await page.evaluate(() => !!document.activeElement?.closest('dialog'))).toBe(true)
  await page.keyboard.press('Escape')
  await expect(finish).toBeFocused()
  await nav(page, 'Sessions').click()
  await page.getByRole('link', { name: 'Review research notes', exact: true }).click()
  await ready(page, 'Review research notes')
  await page.getByRole('button', { name: 'Activity aggregates' }).click()
  await expect(page.getByText('editor', { exact: true })).toBeVisible()
  await page.getByRole('button', { name: 'Estimated states' }).click()
  await expect(page.getByText(/^Rule version:/)).toBeVisible()
  await nav(page, 'Analytics').click()
  await page.getByText('Compare reported ratings, estimates and goals', { exact: true }).click()
  await expect(page.getByRole('link', { name: /^Session [a-f\d]{8}$/ })).toHaveCount(2)
  await noOverflow(page)

  if (process.env.FOCUS_CAPTURE_QA === '1') {
    await page.emulateMedia({ reducedMotion: 'reduce' })
    expect(await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches)).toBe(
      true,
    )
    for (const theme of ['light', 'dark'] as const) {
      if (theme === 'dark') await page.getByRole('button', { name: 'Switch to dark theme' }).click()
      for (const [screen, title, open] of [
        ['dashboard', 'Overview', () => nav(page, 'Overview').click()],
        ['history', 'Sessions', () => nav(page, 'Sessions').click()],
        [
          'creation',
          'New session',
          () => page.getByRole('link', { name: 'New session', exact: true }).click(),
        ],
        [
          'active',
          'Draft the research methods',
          async () => {
            await nav(page, 'Sessions').click()
            await page
              .getByRole('link', { name: 'Draft the research methods', exact: true })
              .click()
          },
        ],
        [
          'completed',
          'Review research notes',
          async () => {
            await nav(page, 'Sessions').click()
            await page.getByRole('link', { name: 'Review research notes', exact: true }).click()
          },
        ],
        ['analytics', 'Analytics', () => nav(page, 'Analytics').click()],
        ['settings', 'Settings', () => nav(page, 'Settings').click()],
      ] as const) {
        await open()
        await ready(page, title)
        await expect(page.getByRole('heading', { name: title, exact: true })).toBeInViewport()
        await auditAxe(page)
        await captureQa(page, 'mobile-' + theme + '-' + screen)
      }
    }
    await page.getByRole('button', { name: 'Switch to light theme' }).click()
  }

  if (process.env.FOCUS_CAPTURE_DESIGN === '1') {
    const folder = resolve('../artifacts/frontend-design')
    await mkdir(folder, { recursive: true })
    const devices = [
      { name: 'mobile', width: 390, height: 844 },
      { name: 'tablet', width: 768, height: 1024 },
      { name: 'desktop', width: 1440, height: 1000 },
    ]
    for (const { name, ...viewport } of devices) {
      await page.setViewportSize(viewport)
      for (const [section, filename, title] of [
        ['Overview', 'dashboard', 'Overview'],
        ['Sessions', 'sessions', 'Sessions'],
        ['Sessions', 'active', 'Draft the research methods'],
        ['Analytics', 'analytics', 'Analytics'],
        ['Settings', 'settings', 'Settings'],
      ] as const) {
        await nav(page, section).click()
        if (filename === 'active')
          await page.getByRole('link', { name: title, exact: true }).click()
        else await ready(page, title)
        await ready(page, title)
        await page.screenshot({
          path: resolve(folder, name + '-' + filename + '.png'),
          animations: 'disabled',
        })
      }
    }
  }
})
