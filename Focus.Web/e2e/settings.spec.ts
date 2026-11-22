import { expect, test } from '@playwright/test'
import { nav, registerByApi, signIn } from './support'

test('saves backend settings, exports real data and deletes the isolated test account', async ({
  page,
  request,
}) => {
  const account = await registerByApi(request, 'Settings Tester')
  await page.goto('/login')
  const login = await signIn(page, account)
  await nav(page, 'Settings').click()
  const tabs = page.getByRole('navigation', { name: 'Settings sections' })
  await tabs.getByRole('link', { name: 'Work profiles' }).click()
  await page.getByRole('button', { name: 'New profile' }).click()
  const profile = page.getByRole('dialog')
  await profile.getByLabel('Profile name').fill('Research')
  await profile.getByRole('button', { name: 'Add rule' }).click()
  await profile.getByLabel('Process name 1').fill('editor')
  await profile.getByLabel('Category 1').selectOption('Allowed')
  await profile.getByRole('button', { name: 'Save profile' }).click()
  await expect(page.getByRole('heading', { name: 'Research' })).toBeVisible()

  await tabs.getByRole('link', { name: 'Pomodoro' }).click()
  await page.getByLabel('Focus duration (minutes)').fill('35')
  await page.getByRole('button', { name: 'Save Pomodoro settings' }).click()
  await expect(page.getByRole('status')).toContainText('Changes saved')

  await tabs.getByRole('link', { name: 'Assistant' }).click()
  await page.getByLabel('Visibility').selectOption('Hidden')
  await page.getByRole('button', { name: 'Save assistant settings' }).click()
  await expect(page.getByRole('status')).toContainText('Changes saved')

  await tabs.getByRole('link', { name: 'Privacy' }).click()
  const telemetry = page.getByRole('checkbox', { name: 'Allow aggregate telemetry' })
  await expect(telemetry).not.toBeChecked()
  await telemetry.click()
  await expect(page.getByRole('status')).toContainText('Changes saved')
  await expect(telemetry).toBeChecked()
  const file = page.waitForEvent('download')
  await page.getByRole('button', { name: 'Download account data' }).click()
  expect((await file).suggestedFilename()).toBe('focus-assistant-export.json')

  const credentials: { accessToken: string } = await login.json()
  const headers = { Authorization: 'Bearer ' + credentials.accessToken }
  const saved = await request.get('/api/v1/settings/pomodoro', { headers })
  expect(await saved.json()).toMatchObject({ focusMinutes: 35 })
  const privacy = await request.get('/api/v1/settings/privacy', { headers })
  expect(await privacy.json()).toMatchObject({ telemetryEnabled: true })

  await page.getByRole('button', { name: 'Delete my account' }).click()
  const confirm = page.getByRole('dialog')
  await expect(confirm.getByRole('button', { name: 'Permanently delete account' })).toBeDisabled()
  await confirm.getByLabel('Type DELETE to confirm').fill('DELETE')
  await confirm.getByLabel('Current password').fill(account.password)
  await confirm.getByRole('button', { name: 'Permanently delete account' }).click()
  await expect(page).toHaveURL(/\/login$/)
  expect((await request.get('/api/v1/auth/me', { headers })).status()).toBe(401)
})
