import { api, read } from '../../api/client'
import { sessions } from '../sessions/data'
import { profiles } from './data'

async function allPages<T>(
  get: (page: number) => Promise<{ items: T[]; pageSize: number; totalCount: number }>,
) {
  const first = await get(1)
  const output = [...first.items]
  for (let page = 2; (page - 1) * first.pageSize < first.totalCount; page++) {
    const result = await get(page)
    output.push(...result.items)
    if (!result.items.length) break
  }
  return output
}

export async function downloadAccount(signal: AbortSignal) {
  const [account, workProfiles, sessionItems] = await Promise.all([
    read(api.GET('/api/v1/settings/privacy/export', { signal })),
    allPages((page) => profiles.list(page, signal)),
    allPages((Page) => sessions.list({ Page, PageSize: 25 }, signal)),
  ])
  const research = []
  for (const session of sessionItems) {
    if (signal.aborted) throw signal.reason
    const [activity, estimates, timeline] = await Promise.all([
      allPages((page) => sessions.activity(session.id, page, signal)),
      allPages((page) => sessions.estimates(session.id, page, signal)),
      sessions.timeline(session.id, 1, signal),
    ])
    research.push({ ...session, activity, estimates, pauses: timeline.pauses })
  }
  if (signal.aborted) throw signal.reason
  const blob = new Blob(
    [
      JSON.stringify(
        { exportedAt: new Date().toISOString(), account, workProfiles, sessions: research },
        null,
        2,
      ),
    ],
    { type: 'application/json' },
  )
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = 'focus-assistant-export.json'
  link.click()
  window.setTimeout(() => URL.revokeObjectURL(url), 2000)
}
