import { expect, test } from '@playwright/test'
import { credentials, registerByApi, signIn } from './support'

test('registers, authenticates and signs out against the real backend', async ({ page }) => {
  const account = credentials('Taylor Researcher')
  await page.goto('/register')
  await page.getByLabel('Display name').fill(account.displayName)
  await page.getByLabel('Email', { exact: true }).fill(account.email)
  await page.getByLabel('Password', { exact: true }).fill(account.password)
  await page.getByLabel('Confirm password').fill(account.password)
  await page.getByRole('button', { name: 'Create account', exact: true }).click()
  await expect(page.getByRole('status')).toContainText('Account created')
  await signIn(page, account)
  await expect(page.getByRole('heading', { name: 'Overview' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Welcome, Taylor Researcher' })).toBeVisible()
  expect(
    await page.evaluate(() => ({
      local: Object.keys(localStorage),
      session: Object.keys(sessionStorage),
    })),
  ).toEqual({ local: ['focus-assistant-theme'], session: [] })
  await page
    .getByRole('navigation', { name: 'Main navigation' })
    .getByRole('link', { name: 'Settings' })
    .click()
  await expect(page.getByText(account.email, { exact: true })).toBeVisible()
  await page.getByRole('button', { name: 'Sign out' }).click()
  await expect(page).toHaveURL(/\/login$/)
  await page.goto('/sessions')
  await expect(page).toHaveURL(/\/login$/)
})

test('completes a real focus session, then finds its persisted goals and ratings', async ({
  page,
  request,
}) => {
  const account = await registerByApi(request, 'Session Tester')
  await page.goto('/login')
  await signIn(page, account)
  await page.getByRole('link', { name: 'New session' }).click()
  await page.getByLabel('Planned duration (minutes)').fill('45')
  await page.getByRole('button', { name: 'Add goal', exact: true }).click()
  await page.getByRole('textbox', { name: 'Goal 1', exact: true }).fill('Review the research notes')
  await page.getByRole('button', { name: 'Create session' }).click()
  await expect(
    page.getByRole('heading', { name: 'Review the research notes', exact: true }),
  ).toBeVisible()

  await page.getByRole('button', { name: 'Add goal', exact: true }).click()
  await page.getByRole('dialog').getByLabel('Goal description').fill('Summarize findings')
  await page.getByRole('button', { name: 'Save goal' }).click()
  await expect(page.locator('.goal-list').getByText('Summarize findings')).toBeVisible()
  await page.getByRole('button', { name: 'Start session' }).click()
  await page.getByRole('button', { name: 'Pause', exact: true }).click()
  await page.getByRole('button', { name: 'Resume', exact: true }).click()
  await page.getByRole('button', { name: 'Finish session' }).click()
  const dialog = page.getByRole('dialog')
  await dialog.getByLabel('Concentration').selectOption('4')
  await dialog.getByLabel('Productivity').selectOption('3')
  await dialog.getByLabel('Outcome for Review the research notes').selectOption('Completed')
  await dialog.getByLabel('Outcome for Summarize findings').selectOption('PartiallyCompleted')
  await dialog.getByLabel('Reflection').fill('A sustained stretch of focused work.')
  await dialog.getByRole('button', { name: 'Save completion' }).click()
  await expect(page.getByRole('heading', { name: 'Completed session' })).toBeVisible()
  const detail = page.url()
  await page.getByRole('link', { name: 'Session history' }).click()
  await page.getByLabel('Status', { exact: true }).selectOption('Completed')
  await page.getByLabel('Date order').selectOption('asc')
  await expect(page.getByRole('table').getByText('Completed', { exact: true })).toBeVisible()

  await page.goto(detail)
  await expect(page).toHaveURL(/\/login$/)
  await signIn(page, account)
  await expect(page).toHaveURL(detail)
  await expect(page.getByText('A sustained stretch of focused work.')).toBeVisible()
  await expect(page.getByText('4 / 5', { exact: true })).toBeVisible()
  await expect(page.getByText('3 / 5', { exact: true })).toBeVisible()
})
