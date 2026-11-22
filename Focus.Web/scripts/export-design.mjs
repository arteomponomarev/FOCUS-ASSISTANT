import { mkdir, readFile } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import { chromium } from '@playwright/test'

const captures = new URL('../../artifacts/frontend-design/', import.meta.url)
const destination = new URL('../../docs/design/', import.meta.url)
const devices = [
  ['mobile', 'Mobile', '390 × 844'],
  ['tablet', 'Tablet', '768 × 1024'],
  ['desktop', 'Desktop', '1440 × 1000'],
]
const screens = [
  ['dashboard', 'Overview', 'Recent sessions, reported ratings and time summaries.'],
  [
    'sessions',
    'Session history',
    'Date and status filters, mobile cards and the desktop data table.',
  ],
  ['active', 'Active session', 'Goal progress, active timer and session controls.'],
  [
    'analytics',
    'Analytics',
    'Time-bucket controls, estimated state distribution and report metrics.',
  ],
  [
    'settings',
    'Settings',
    'Account settings and navigation to work profiles, privacy and assistant preferences.',
  ],
]
const plates = await Promise.all(
  screens.map(async ([name, title, description]) => {
    const figures = await Promise.all(
      devices.map(async ([device, label, dimensions]) => {
        const png = await readFile(new URL(device + '-' + name + '.png', captures))
        return (
          '<figure><figcaption>' +
          label +
          ' · ' +
          dimensions +
          ' CSS px</figcaption><img alt="' +
          title +
          ' at ' +
          label +
          ' width" src="data:image/png;base64,' +
          png.toString('base64') +
          '"></figure>'
        )
      }),
    )
    return (
      '<section><h1>' +
      title +
      '</h1><p>' +
      description +
      '</p><div class="devices">' +
      figures.join('') +
      '</div></section>'
    )
  }),
)
const system = `<section><h1>Focus Assistant design system</h1>
<p>Live Chrome captures use synthetic records persisted by the test ASP.NET Core API in an isolated PostgreSQL database.</p>
<h2>Color and typography</h2>
<div class="swatches"><span style="background:#5232cd;color:white">Primary · #5232CD</span>
<span style="background:#f6f5fa">Canvas · #F6F5FA</span><span style="background:#e9fd9a">Accent · #E9FD9A</span>
<span style="background:#e3f7eb">Success · #E3F7EB</span><span style="background:#fcebf0">Error · #FCEBF0</span></div>
<p>System sans-serif · body 1 rem · headings 1.8–3 rem · 1.125 rem card corners · 0.875–2 rem fluid content spacing.</p>
<h2>Interaction states</h2>
<div class="states"><span>Start session</span><span style="background:#4325af">Hover</span>
<span style="outline:3px solid #5331d6;outline-offset:3px">Keyboard focus</span>
<span style="background:#eeebf6;color:#615a75">Unavailable</span>
<span style="background:#eeebf6;color:#615a75">Loading…</span>
<span style="background:#fcebf0;color:#932744">Check the marked fields</span></div>
<h2>Responsive navigation</h2>
<p>Up to 42 rem, use the bottom navigation and stacked layouts. Through 63.99 rem, use a condensed sidebar and flexible columns. At desktop widths, use the labeled sidebar and wider grids. Shared controls have a 2.75 rem minimum size.</p></section>`
const css = `@page {size:A4 landscape;margin:10mm} *{box-sizing:border-box} body{font:12px/1.45 Arial,sans-serif;color:#231d35;margin:0}
section{break-after:page} section:last-child{break-after:auto} h1{font-size:25px;margin:0} h2{margin:23px 0 8px}
p{margin:4px 0 10px;color:#615a75} .devices{display:grid;grid-template-columns:1fr 1.3fr 2.15fr;gap:12px;align-items:start}
figure{margin:0;min-width:0} figcaption{font-weight:bold;margin:3px 0 6px} img{width:100%;max-height:155mm;object-fit:contain;object-position:left top;border:1px solid #ded9eb;border-radius:5px}
.swatches,.states{display:flex;flex-wrap:wrap;gap:10px;margin:10px 0 14px}.swatches span,.states span{padding:12px;border-radius:8px;border:1px solid #ded9eb}
.states span{background:#5232cd;color:white}`
const browser = await chromium.launch({
  ...(process.env.FOCUS_BROWSER_PATH ? { executablePath: process.env.FOCUS_BROWSER_PATH } : {}),
})
try {
  const page = await browser.newPage()
  await page.setContent(
    '<!doctype html><html lang="en"><title>Focus Assistant Adaptive UI</title><style>' +
      css +
      '</style><body>' +
      system +
      plates.join('') +
      '</body></html>',
  )
  await mkdir(destination, { recursive: true })
  await page.pdf({
    path: fileURLToPath(new URL('focus-assistant-adaptive-ui.pdf', destination)),
    printBackground: true,
    preferCSSPageSize: true,
    tagged: true,
  })
} finally {
  await browser.close()
}
