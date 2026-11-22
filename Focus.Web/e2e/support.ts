import { randomBytes } from 'node:crypto'
import { expect } from '@playwright/test'
import type { APIRequestContext, Page } from '@playwright/test'

export function credentials(name: string) {
  return {
    displayName: name,
    email: 'browser-' + randomBytes(8).toString('hex') + '@example.invalid',
    password: randomBytes(20).toString('hex'),
  }
}

export async function registerByApi(request: APIRequestContext, name: string) {
  const account = credentials(name)
  expect((await request.post('/api/v1/auth/register', { data: account })).status()).toBe(201)
  return account
}

export async function signIn(page: Page, account: { email: string; password: string }) {
  await page.getByLabel('Email', { exact: true }).fill(account.email)
  await page.getByLabel('Password', { exact: true }).fill(account.password)
  const [response] = await Promise.all([
    page.waitForResponse((reply) => new URL(reply.url()).pathname === '/api/v1/auth/login'),
    page.getByRole('button', { name: 'Sign in', exact: true }).click(),
  ])
  expect(response.status()).toBe(200)
  return response
}

export function nav(page: Page, name: string) {
  return page
    .getByRole('navigation', { name: 'Main navigation' })
    .getByRole('link', { name, exact: true })
}
