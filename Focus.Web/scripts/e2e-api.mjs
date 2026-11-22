import { randomUUID } from 'node:crypto'
import { existsSync } from 'node:fs'
import { spawn, spawnSync } from 'node:child_process'
import { fileURLToPath } from 'node:url'
import { join, resolve } from 'node:path'

const source = process.env.FOCUS_WEB_TEST_DATABASE
if (!source) throw new Error('Set FOCUS_WEB_TEST_DATABASE to a disposable PostgreSQL cluster.')

const parameters = source
  .split(';')
  .filter(Boolean)
  .map((entry) => {
    const equals = entry.indexOf('=')
    return [entry.slice(0, equals).trim().toLowerCase(), entry.slice(equals + 1)]
  })
const settings = new Map(parameters)
const allowed = ['host', 'port', 'database', 'username', 'password']
if (
  settings.size !== parameters.length ||
  [...settings.keys()].some((key) => !allowed.includes(key)) ||
  !['127.0.0.1', 'localhost', '::1'].includes(settings.get('host')) ||
  !/^focus_test_[a-z0-9_]+$/.test(settings.get('database') || '')
) {
  throw new Error('Use an isolated loopback cluster with a focus_test_ database prefix.')
}
settings.set('database', 'focus_test_web_' + randomUUID().replaceAll('-', ''))
const api = fileURLToPath(new URL('../../Focus.Api/', import.meta.url))
const dll = process.env.FOCUS_WEB_API_DLL
  ? resolve(process.env.FOCUS_WEB_API_DLL)
  : join(api, 'bin', 'Release', 'net10.0', 'Focus.Api.dll')
if (!existsSync(dll)) throw new Error('Build Focus.Api in Release before running browser tests.')

const env = Object.fromEntries(
  Object.entries(process.env).filter(
    ([key]) =>
      !/^(ASPNETCORE_|DOTNET_ENVIRONMENT|CONNECTIONSTRINGS__|KESTREL__|AUTHENTICATION__|OBSERVABILITY__|OTEL_|FOCUS_WEB_TEST_DATABASE)/i.test(
        key,
      ),
  ),
)
Object.assign(env, {
  ASPNETCORE_ENVIRONMENT: 'Contract',
  DOTNET_ENVIRONMENT: 'Contract',
  ASPNETCORE_URLS: 'http://127.0.0.1:5188',
  ConnectionStrings__PostgreSQL: [...settings].map(([key, value]) => key + '=' + value).join(';'),
})
const migration = spawnSync('dotnet', [dll, '--migrate'], { cwd: api, env, stdio: 'inherit' })
if (migration.status !== 0) process.exit(migration.status ?? 1)

const child = spawn('dotnet', [dll], { cwd: api, env, stdio: 'inherit' })
child.on('error', () => process.exit(1))
child.on('exit', (code) => process.exit(code ?? 0))
for (const signal of ['SIGINT', 'SIGTERM']) process.on(signal, () => child.kill(signal))
