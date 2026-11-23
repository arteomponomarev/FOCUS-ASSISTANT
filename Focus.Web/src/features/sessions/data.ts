import { api, done, read } from '../../api/client'
import type { Model } from '../../api/client'
import { focusApi, mutationRequest, queryRequest } from '../../api/state'

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
type Transition = 'start' | 'pause' | 'resume' | 'cancel'
type SignalPage = { id: string; page: number }
type GoalChange = { id: string } & (
  | { kind: 'add'; description: string }
  | { kind: 'edit'; goalId: string; body: Model['UpdateGoalRequest'] }
  | { kind: 'remove'; goalId: string }
)

export const sessions = {
  list: (query: SessionQuery, signal?: AbortSignal) =>
    read(api.GET('/api/v1/sessions', { params: { query }, signal })),
  get: (id: string, signal?: AbortSignal) =>
    read(api.GET('/api/v1/sessions/{id}', { params: { path: { id } }, signal })),
  create: (body: Model['CreateSessionRequest'], signal?: AbortSignal) =>
    read(api.POST('/api/v1/sessions', { body, signal })),
  complete: (id: string, body: Model['CompleteSessionRequest'], signal?: AbortSignal) =>
    read(api.POST('/api/v1/sessions/{id}/complete', { params: { path: { id } }, body, signal })),
  transition: (id: string, action: Transition, signal?: AbortSignal) =>
    read(
      api.POST(
        ('/api/v1/sessions/{id}/' + action) as
          | '/api/v1/sessions/{id}/start'
          | '/api/v1/sessions/{id}/pause'
          | '/api/v1/sessions/{id}/resume'
          | '/api/v1/sessions/{id}/cancel',
        { params: { path: { id } }, signal },
      ),
    ),
  addGoal: (id: string, description: string, signal?: AbortSignal) =>
    read(
      api.POST('/api/v1/sessions/{id}/goals', {
        params: { path: { id } },
        body: { description },
        signal,
      }),
    ),
  editGoal: (id: string, goalId: string, body: Model['UpdateGoalRequest'], signal?: AbortSignal) =>
    read(
      api.PATCH('/api/v1/sessions/{id}/goals/{goalId}', {
        params: { path: { id, goalId } },
        body,
        signal,
      }),
    ),
  deleteGoal: (id: string, goalId: string, signal?: AbortSignal) =>
    done(
      api.DELETE('/api/v1/sessions/{id}/goals/{goalId}', {
        params: { path: { id, goalId } },
        signal,
      }),
    ),
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
  vision: (id: string, signal?: AbortSignal) =>
    read(api.GET('/api/v1/sessions/{id}/vision', { params: { path: { id } }, signal })),
}

const collections = ['Sessions', 'Analytics'] as const
const sessionTags = (id: string) => [
  ...collections,
  { type: 'Session' as const, id },
  { type: 'Signals' as const, id },
]

export const sessionApi = focusApi.injectEndpoints({
  endpoints: (build) => ({
    sessionList: build.query<Model['PageOfSessionResponse'], SessionQuery>({
      queryFn: queryRequest(sessions.list),
      providesTags: ['Sessions'],
    }),
    session: build.query<Session, string>({
      queryFn: queryRequest(sessions.get),
      providesTags: (_data, _error, id) => [{ type: 'Session', id }],
    }),
    createSession: build.mutation<Session, Model['CreateSessionRequest']>({
      queryFn: mutationRequest(sessions.create),
      invalidatesTags: (_data, error) => (error ? [] : collections),
    }),
    transitionSession: build.mutation<Session, { id: string; action: Transition }>({
      queryFn: mutationRequest(({ id, action }, signal) => sessions.transition(id, action, signal)),
      invalidatesTags: (_data, error, { id }) => (error ? [] : sessionTags(id)),
    }),
    completeSession: build.mutation<Session, { id: string; body: Model['CompleteSessionRequest'] }>(
      {
        queryFn: mutationRequest(({ id, body }, signal) => sessions.complete(id, body, signal)),
        invalidatesTags: (_data, error, { id }) => (error ? [] : sessionTags(id)),
      },
    ),
    changeGoal: build.mutation<Model['GoalResponse'] | null, GoalChange>({
      queryFn: mutationRequest(async (change, signal) => {
        switch (change.kind) {
          case 'add':
            return sessions.addGoal(change.id, change.description, signal)
          case 'edit':
            return sessions.editGoal(change.id, change.goalId, change.body, signal)
          case 'remove':
            await sessions.deleteGoal(change.id, change.goalId, signal)
            return null
        }
      }),
      invalidatesTags: (_data, error, { id }) => (error ? [] : sessionTags(id)),
    }),
    activity: build.query<Model['PageOfActivityResponse'], SignalPage>({
      queryFn: queryRequest(({ id, page }, signal) => sessions.activity(id, page, signal)),
      providesTags: (_data, _error, { id }) => [{ type: 'Signals', id }],
    }),
    estimates: build.query<Model['PageOfEstimateResponse'], SignalPage>({
      queryFn: queryRequest(({ id, page }, signal) => sessions.estimates(id, page, signal)),
      providesTags: (_data, _error, { id }) => [{ type: 'Signals', id }],
    }),
    timeline: build.query<Model['TimelineResponse'], SignalPage>({
      queryFn: queryRequest(({ id, page }, signal) => sessions.timeline(id, page, signal)),
      providesTags: (_data, _error, { id }) => [{ type: 'Signals', id }],
    }),
    vision: build.query<Model['VisionReport'], string>({
      queryFn: queryRequest(sessions.vision),
      providesTags: (_data, _error, id) => [{ type: 'Signals', id }],
    }),
  }),
})

export const {
  useSessionListQuery,
  useSessionQuery,
  useCreateSessionMutation,
  useTransitionSessionMutation,
  useCompleteSessionMutation,
  useChangeGoalMutation,
  useActivityQuery,
  useEstimatesQuery,
  useTimelineQuery,
  useVisionQuery,
} = sessionApi

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
