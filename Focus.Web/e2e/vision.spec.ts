import { expect, test } from '@playwright/test'
import { nav, registerByApi, signIn } from './support'

test('displays consented synthetic camera totals and preserves independent desktop consent', async ({
  page,
  request,
}) => {
  const account = await registerByApi(request, 'Camera research')
  await page.goto('/login')
  const { accessToken } = (await (await signIn(page, account)).json()) as { accessToken: string }
  const headers = { Authorization: 'Bearer ' + accessToken }

  await nav(page, 'Settings').click()
  await page.getByRole('link', { name: 'Privacy', exact: true }).click()
  const camera = page.getByRole('checkbox', { name: 'Allow coarse camera observation totals' })
  await expect(camera).not.toBeChecked()
  const consent = page.waitForResponse(
    (response) =>
      response.request().method() === 'PATCH' &&
      new URL(response.url()).pathname === '/api/v1/settings/privacy',
  )
  await camera.check()
  expect((await consent).status()).toBe(200)
  await expect(camera).toBeChecked()
  await expect(page.getByRole('checkbox', { name: 'Allow aggregate telemetry' })).not.toBeChecked()

  const created = await request.post('/api/v1/sessions', {
    headers,
    data: { plannedDurationSeconds: 20, goals: ['Camera observation test'] },
  })
  expect(created.status()).toBe(201)
  const { id } = (await created.json()) as { id: string }
  const path = '/api/v1/sessions/' + id
  const start = await request.post(path + '/start', { headers })
  expect(start.status()).toBe(200)
  const { startedAt } = (await start.json()) as { startedAt: string }
  const from = Date.parse(startedAt) + 1
  await nav(page, 'Sessions').click()
  await page.getByRole('link', { name: 'Camera observation test', exact: true }).click()
  await page.getByRole('button', { name: 'Computer Vision', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'No camera observations uploaded' })).toBeVisible()
  await new Promise((resolve) => setTimeout(resolve, Math.max(0, from + 550 - Date.now())))
  const uploaded = await request.post(path + '/activity/batch', {
    headers,
    data: {
      items: [
        {
          clientEventId: globalThis.crypto.randomUUID(),
          startedAt: new Date(from).toISOString(),
          endedAt: new Date(from + 500).toISOString(),
          keyboardEventCount: 0,
          mouseClickCount: 0,
          windowSwitchCount: 0,
          idleSeconds: 0,
          vision: {
            presentMs: 400,
            notDetectedMs: 0,
            unknownMs: 100,
            facingMs: 300,
            awayMs: 100,
            validSamples: 2,
          },
        },
      ],
    },
  })
  expect(uploaded.status(), await uploaded.text()).toBe(200)
  await expect(page.getByText('Face detected in usable observations')).toBeVisible()
  await expect(page.getByText('100%', { exact: true })).toBeVisible()
  await expect(page.getByText('75%', { exact: true })).toBeVisible()
})
