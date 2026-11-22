import { api, done, read } from '../../api/client'
import type { Model } from '../../api/client'
import type { QueryClient } from '@tanstack/react-query'

export const sessionStatuses = [
  'Planned',
  'Active',
  'Paused',
  'Completed',
  'Cancelled',
] as const satisfies Model['SessionStatus'][]
export const goalOutcomes = [
  'Completed',
  'PartiallyCompleted',
  'NotCompleted',
] as const satisfies Model['GoalStatus'][]
export type GoalOutcome = (typeof goalOutcomes)[number]
export type Session = Model['SessionResponse']
export type SessionQuery = {
  From?: string
  To?: string
  Status?: Model['SessionStatus']
  Sort?: 'asc' | 'desc'
  Page?: number
  PageSize?: number
}
export const detailKey = (id: string) => ['session', id] as const

export function refreshSessionCollections(cache: QueryClient) {
  for (const queryKey of [['sessions'], ['analytics']]) {
    cache.removeQueries({ queryKey, type: 'inactive' })
    void cache.invalidateQueries({ queryKey, type: 'active' })
  }
}

export const sessions = {
  list: (query: SessionQuery, signal?: AbortSignal) =>
    read(api.GET('/api/v1/sessions', { params: { query }, signal })),
  get: (id: string, signal?: AbortSignal) =>
    read(api.GET('/api/v1/sessions/{id}', { params: { path: { id } }, signal })),
  create: (body: Model['CreateSessionRequest']) => read(api.POST('/api/v1/sessions', { body })),
  complete: (id: string, body: Model['CompleteSessionRequest']) =>
    read(api.POST('/api/v1/sessions/{id}/complete', { params: { path: { id } }, body })),
  transition: (id: string, action: 'start' | 'pause' | 'resume' | 'cancel') =>
    read(
      api.POST(
        ('/api/v1/sessions/{id}/' + action) as
          | '/api/v1/sessions/{id}/start'
          | '/api/v1/sessions/{id}/pause'
          | '/api/v1/sessions/{id}/resume'
          | '/api/v1/sessions/{id}/cancel',
        { params: { path: { id } } },
      ),
    ),
  addGoal: (id: string, description: string) =>
    read(
      api.POST('/api/v1/sessions/{id}/goals', { params: { path: { id } }, body: { description } }),
    ),
  editGoal: (id: string, goalId: string, body: Model['UpdateGoalRequest']) =>
    read(
      api.PATCH('/api/v1/sessions/{id}/goals/{goalId}', { params: { path: { id, goalId } }, body }),
    ),
  deleteGoal: (id: string, goalId: string) =>
    done(api.DELETE('/api/v1/sessions/{id}/goals/{goalId}', { params: { path: { id, goalId } } })),
  activity: (id: string, Page: number, signal?: AbortSignal) =>
    read(
      api.GET('/api/v1/sessions/{id}/activity', {
        params: { path: { id }, query: { Page, PageSize: 10 } },
        signal,
      }),
    ),
  estimates: (id: string, Page: number, signal?: AbortSignal) =>
    read(
      api.GET('/api/v1/sessions/{id}/focus-states', {
        params: { path: { id }, query: { Page, PageSize: 10 } },
        signal,
      }),
    ),
  timeline: (id: string, Page: number, signal?: AbortSignal) =>
    read(
      api.GET('/api/v1/sessions/{id}/timeline', {
        params: { path: { id }, query: { Page, PageSize: 10 } },
        signal,
      }),
    ),
}

export function sessionTitle(session: Session) {
  return session.goals[0]?.description || 'Session ' + session.id.slice(0, 8)
}

export function goalLabel(status: Model['GoalStatus']) {
  return status === 'NotCompleted'
    ? 'Not completed'
    : status === 'PartiallyCompleted'
      ? 'Partially completed'
      : status
}
