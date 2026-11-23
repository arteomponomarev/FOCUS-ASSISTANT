import { spawn, spawnSync } from 'node:child_process'
import { randomUUID } from 'node:crypto'
import { mkdir, writeFile } from 'node:fs/promises'
import { createRequire } from 'node:module'
import { join, resolve } from 'node:path'
import { pathToFileURL } from 'node:url'

const origin = 'http://127.0.0.1:4197'
const apiOrigin = 'http://127.0.0.1:5197'
if (!process.env.FOCUS_LIGHTHOUSE_PACKAGES || !process.env.FOCUS_BROWSER_PATH) {
  throw new Error('Set FOCUS_LIGHTHOUSE_PACKAGES and FOCUS_BROWSER_PATH for the isolated audit.')
}
const packages = resolve(process.env.FOCUS_LIGHTHOUSE_PACKAGES)
const require = createRequire(join(packages, 'package.json'))
const { startFlow } = await import(pathToFileURL(require.resolve('lighthouse')).href)
const { default: puppeteer } = await import(pathToFileURL(require.resolve('puppeteer-core')).href)
const { default: desktopConfig } = await import(
  pathToFileURL(join(packages, 'node_modules/lighthouse/core/config/desktop-config.js')).href
)

async function reached(url) {
  try {
    return (await fetch(url, { signal: AbortSignal.timeout(800) })).ok
  } catch {
    return false
  }
}

async function ready(child, url) {
  for (let attempt = 0; attempt < 150; attempt++) {
    if (child.exitCode !== null) throw new Error('Fixture stopped during startup.')
    if (await reached(url)) return
    await new Promise((done) => setTimeout(done, 200))
  }
  throw new Error('Fixture health check timed out.')
}

function exit(child) {
  if (!child?.pid || child.exitCode !== null) return
  if (process.platform === 'win32') {
    spawnSync('taskkill.exe', ['/pid', String(child.pid), '/t', '/f'], {
      windowsHide: true,
      stdio: 'ignore',
    })
  } else {
    child.kill('SIGTERM')
  }
}

async function seed() {
  const email = 'lh-' + randomUUID() + '@example.invalid'
  const password = randomUUID()
  async function post(path, body, token) {
    const response = await fetch(origin + '/api/v1' + path, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: 'Bearer ' + token } : {}),
      },
      body: body && JSON.stringify(body),
      signal: AbortSignal.timeout(10_000),
    })
    if (!response.ok) throw new Error('Synthetic seed failed: HTTP ' + response.status)
    return response.json()
  }
  await post('/auth/register', { email, password, displayName: 'Audit fixture' })
  const { accessToken } = await post('/auth/login', { email, password })
  const session = await post(
    '/sessions',
    { plannedDurationSeconds: 1800, goals: ['Review methods'] },
    accessToken,
  )
  await post('/sessions/' + session.id + '/start', undefined, accessToken)
  await post(
    '/sessions/' + session.id + '/complete',
    {
      selfReportedConcentration: 4,
      selfReportedProductivity: 3,
      goalOutcomes: [{ goalId: session.goals[0].id, status: 'Completed' }],
    },
    accessToken,
  )
  return { email, password }
}

async function hydrated(page, pathname) {
  await page.waitForFunction(
    (path) =>
      location.pathname === path &&
      document.querySelector('h1') &&
      !document.querySelector('.skeleton-wrap, .busy'),
    { timeout: 15000 },
    pathname,
  )
}

async function measure(profile, account) {
  const browser = await puppeteer.launch({
    executablePath: process.env.FOCUS_BROWSER_PATH,
    headless: true,
  })
  try {
    const page = await browser.newPage()
    const desktop = profile === 'desktop'
    const flow = await startFlow(page, desktop ? { config: desktopConfig } : {})
    await flow.navigate(origin + '/login', { stepName: 'Login cold navigation' })
    await page.setViewport(
      desktop
        ? { width: 1440, height: 1000 }
        : { width: 390, height: 844, deviceScaleFactor: 2, isMobile: true, hasTouch: true },
    )
    await page.goto(origin + '/login', { waitUntil: 'domcontentloaded' })
    await page.waitForSelector('input[type=email]', { visible: true })
    await page.type('input[type=email]', account.email)
    await page.type('input[type=password]', account.password)
    await page.click('button[type=submit]')
    await hydrated(page, '/dashboard')
    await flow.snapshot({ stepName: 'Authenticated overview' })
    for (const [path, label] of [
      ['/sessions', 'Sessions'],
      ['/analytics', 'Analytics'],
      ['/settings', 'Settings'],
      ['/dashboard', 'Overview return'],
    ]) {
      await flow.startTimespan({ stepName: label + ' transition' })
      await page.click('nav[aria-label="Main navigation"] a[href="' + path + '"]')
      await hydrated(page, path)
      await flow.endTimespan()
      await flow.snapshot({ stepName: label + ' authenticated' })
    }
    const result = await flow.createFlowResult()
    return result.steps.map(({ lhr, name }) => ({
      name,
      mode: lhr.gatherMode,
      navigationPerformance:
        lhr.gatherMode === 'navigation' ? (lhr.categories.performance?.score ?? null) : null,
      accessibility: lhr.categories.accessibility?.score ?? null,
      lcpMs: lhr.audits['largest-contentful-paint']?.numericValue ?? null,
      blockingMs: lhr.audits['total-blocking-time']?.numericValue ?? null,
      cls: lhr.audits['cumulative-layout-shift']?.numericValue ?? null,
      requests: lhr.audits['network-requests']?.details?.items?.length ?? null,
    }))
  } finally {
    await browser.close()
  }
}

if ((await reached(origin + '/login')) || (await reached(apiOrigin + '/health/live'))) {
  throw new Error('Ports 4197 and 5197 must be unused before starting the isolated audit.')
}

let api
let web
try {
  const childOptions = { windowsHide: true, stdio: 'ignore' }
  api = spawn(process.execPath, ['scripts/e2e-api.mjs'], {
    ...childOptions,
    env: { ...process.env, FOCUS_WEB_FIXTURE_PORT: '5197' },
  })
  await ready(api, apiOrigin + '/health/ready')
  web = spawn(
    process.execPath,
    [
      'node_modules/vite/bin/vite.js',
      'preview',
      '--host',
      '127.0.0.1',
      '--port',
      '4197',
      '--strictPort',
    ],
    { ...childOptions, env: { ...process.env, FOCUS_API_ORIGIN: apiOrigin } },
  )
  await ready(web, origin + '/login')
  const results = {}
  for (const profile of ['mobile', 'desktop']) {
    results[profile] = await measure(profile, await seed())
  }
  const folder = resolve('../artifacts/task20')
  await mkdir(folder, { recursive: true })
  await writeFile(
    join(folder, 'lighthouse-user-flow.json'),
    JSON.stringify(results, null, 2) + '\n',
  )
  console.log(JSON.stringify(results))
} finally {
  exit(web)
  exit(api)
}
