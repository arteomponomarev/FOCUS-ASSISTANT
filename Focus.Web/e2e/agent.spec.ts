import { expect, test } from '@playwright/test'
import { registerByApi, signIn } from './support'

test('shows live aggregates and the final partial window after browser completion', async ({
  page,
  request,
}) => {
  test.setTimeout(65_000)
  const account = await registerByApi(request, 'Desktop Signals')
  await page.goto('/login')
  const login = await signIn(page, account)
  const { accessToken }: { accessToken: string } = await login.json()
  const headers = { Authorization: 'Bearer ' + accessToken }

  await page.getByRole('link', { name: 'New session', exact: true }).click()
  await page.getByRole('button', { name: 'Add goal', exact: true }).click()
  await page.getByRole('textbox', { name: 'Goal 1', exact: true }).fill('Review the short interval')
  await page.getByRole('button', { name: 'Create session' }).click()
  await expect(page.getByRole('heading', { name: 'Review the short interval' })).toBeVisible()
  const detail = page.url()
  const id = new URL(detail).pathname.split('/').at(-1)
  if (!id) throw new Error('Expected a session detail route')
  const route = '/api/v1/sessions/' + id
  await expect(page.getByText('Telemetry is disabled.', { exact: false })).toBeVisible()
  expect(
    (
      await request.patch('/api/v1/settings/privacy', {
        headers,
        data: { telemetryEnabled: true },
      })
    ).status(),
  ).toBe(200)

  const starting = page.waitForResponse(
    (reply) => new URL(reply.url()).pathname === route + '/start',
  )
  await page.getByRole('button', { name: 'Start session' }).click()
  const started: { startedAt: string } = await (await starting).json()
  const start = Date.parse(started.startedAt) + 1
  const firstEnd = start + 15_000
  await page.getByRole('button', { name: 'Activity aggregates' }).click()
  await expect(page.getByRole('heading', { name: 'No desktop signals recorded' })).toBeVisible()
  await new Promise((resolve) => setTimeout(resolve, Math.max(0, firstEnd + 150 - Date.now())))
  const first = {
    clientEventId: globalThis.crypto.randomUUID(),
    startedAt: new Date(start).toISOString(),
    endedAt: new Date(firstEnd).toISOString(),
    activeProcessName: 'editor',
    keyboardEventCount: 7,
    mouseClickCount: 2,
    windowSwitchCount: 1,
    idleSeconds: 0,
  }
  const upload = (item: typeof first) =>
    request.post(route + '/activity/batch', {
      headers,
      data: { items: [item] },
    })
  expect(await (await upload(first)).json()).toMatchObject({ created: 1 })
  await expect(page.getByText('editor', { exact: true })).toBeVisible()

  await page.getByRole('button', { name: 'Finish session' }).click()
  const dialog = page.getByRole('dialog')
  await dialog.getByLabel('Concentration').selectOption('4')
  await dialog.getByLabel('Productivity').selectOption('3')
  await new Promise((resolve) => setTimeout(resolve, Math.max(0, start + 20_000 - Date.now())))
  const finishing = page.waitForResponse(
    (reply) => new URL(reply.url()).pathname === route + '/complete',
  )
  await dialog.getByRole('button', { name: 'Save completion' }).click()
  const completed: { endedAt: string } = await (await finishing).json()
  await expect(page.getByRole('heading', { name: 'Completed session' })).toBeVisible()
  const beforeTail = await page.waitForResponse(
    (reply) =>
      reply.ok() &&
      reply.request().method() === 'GET' &&
      new URL(reply.url()).pathname === route + '/activity',
  )
  expect(await beforeTail.json()).toMatchObject({ totalCount: 1 })
  const tail = {
    ...first,
    clientEventId: globalThis.crypto.randomUUID(),
    startedAt: first.endedAt,
    endedAt: completed.endedAt,
    activeProcessName: 'terminal',
    keyboardEventCount: 0,
    mouseClickCount: 0,
    windowSwitchCount: 0,
  }
  expect(Date.parse(tail.endedAt) - firstEnd).toBeGreaterThanOrEqual(4_500)
  expect(await (await upload(tail)).json()).toMatchObject({ created: 1 })
  expect(await (await upload(tail)).json()).toMatchObject({ created: 0, duplicates: 1 })
  await expect(page.getByText('terminal', { exact: true })).toBeVisible()
  await expect(
    page.locator('.timeline li').filter({ hasText: 'terminal' }).locator('dd').first(),
  ).toHaveText('0')

  await page.reload()
  await expect(page).toHaveURL(/\/login$/)
  await signIn(page, account)
  await expect(page).toHaveURL(detail)
  await page.getByRole('button', { name: 'Estimated states' }).click()
  await expect(page.getByText(/^Rule version:/)).toHaveCount(2)
  await page.getByRole('button', { name: 'Timeline', exact: true }).click()
  await expect(page.locator('.timeline li')).toHaveCount(4)
})
