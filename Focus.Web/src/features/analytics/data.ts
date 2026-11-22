import { api, read } from '../../api/client'
import type { Model } from '../../api/client'

export type ReportQuery = { From?: string; To?: string; TimeZone?: string }
export const stateOrder = [
  'Focused',
  'Neutral',
  'Distracted',
  'Idle',
  'Break',
  'Absent',
] as const satisfies Model['FocusStateType'][]

export const reports = {
  summary: (query: ReportQuery, signal?: AbortSignal) =>
    read(api.GET('/api/v1/analytics/summary', { params: { query }, signal })),
  statistics: (query: ReportQuery & { Period: Model['ReportPeriod'] }, signal?: AbortSignal) =>
    read(api.GET('/api/v1/analytics/statistics', { params: { query }, signal })),
  research: (range: { From?: string; To?: string }, Page: number, signal?: AbortSignal) =>
    read(
      api.GET('/api/v1/analytics/research/sessions', {
        params: { query: { ...range, Page, PageSize: 10, Sort: 'desc' } },
        signal,
      }),
    ),
}

export function estimateDistribution(seconds: Record<string, number>) {
  const total = stateOrder.reduce((sum, state) => sum + (seconds[state] ?? 0), 0)
  return stateOrder.map((state) => ({
    state,
    seconds: seconds[state] ?? 0,
    percent: total > 0 ? ((seconds[state] ?? 0) / total) * 100 : 0,
  }))
}

export const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone
