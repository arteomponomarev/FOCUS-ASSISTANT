import { api, done, read } from '../../api/client'
import type { Model } from '../../api/client'
import { focusApi, mutationRequest, queryRequest } from '../../api/state'

export const profiles = {
  list: (Page = 1, signal?: AbortSignal) =>
    read(
      api.GET('/api/v1/settings/work-profiles', {
        params: { query: { Page, PageSize: 25 } },
        signal,
      }),
    ),
  save: (body: Model['WorkProfileRequest'], id?: string, signal?: AbortSignal) =>
    id
      ? read(
          api.PUT('/api/v1/settings/work-profiles/{id}', {
            params: { path: { id } },
            body,
            signal,
          }),
        )
      : read(api.POST('/api/v1/settings/work-profiles', { body, signal })),
  remove: (id: string, signal?: AbortSignal) =>
    done(api.DELETE('/api/v1/settings/work-profiles/{id}', { params: { path: { id } }, signal })),
}

const settingsApi = focusApi.injectEndpoints({
  endpoints: (build) => ({
    workProfiles: build.query<Model['PageOfWorkProfileResponse'], number>({
      queryFn: queryRequest(profiles.list),
      providesTags: ['WorkProfiles'],
    }),
    workProfilePicker: build.infiniteQuery<Model['PageOfWorkProfileResponse'], void, number>({
      infiniteQueryOptions: {
        initialPageParam: 1,
        getNextPageParam: (last) =>
          last.items.length && last.pageNumber * last.pageSize < last.totalCount
            ? last.pageNumber + 1
            : undefined,
      },
      queryFn: queryRequest(({ pageParam }, signal) => profiles.list(pageParam, signal)),
      providesTags: ['WorkProfiles'],
    }),
    saveWorkProfile: build.mutation<
      Model['WorkProfileResponse'],
      { body: Model['WorkProfileRequest']; id?: string }
    >({
      queryFn: mutationRequest(({ body, id }, signal) => profiles.save(body, id, signal)),
      invalidatesTags: (_data, error) => (error ? [] : ['WorkProfiles']),
    }),
    deleteWorkProfile: build.mutation<null, string>({
      queryFn: mutationRequest(async (id, signal) => {
        await profiles.remove(id, signal)
        return null
      }),
      invalidatesTags: (_data, error) => (error ? [] : ['WorkProfiles']),
    }),
    privacy: build.query<Model['PrivacySettings'], void>({
      queryFn: queryRequest((_, signal) => read(api.GET('/api/v1/settings/privacy', { signal }))),
      providesTags: [{ type: 'Settings', id: 'privacy' }],
    }),
    saveConsent: build.mutation<Model['PrivacySettings'], boolean>({
      queryFn: mutationRequest((telemetryEnabled, signal) =>
        read(api.PATCH('/api/v1/settings/privacy', { body: { telemetryEnabled }, signal })),
      ),
      invalidatesTags: (_data, error) => (error ? [] : [{ type: 'Settings', id: 'privacy' }]),
    }),
    pomodoro: build.query<Model['PomodoroSettings'], void>({
      queryFn: queryRequest((_, signal) => read(api.GET('/api/v1/settings/pomodoro', { signal }))),
      providesTags: [{ type: 'Settings', id: 'pomodoro' }],
    }),
    savePomodoro: build.mutation<Model['PomodoroSettings'], Model['PomodoroSettings']>({
      queryFn: mutationRequest((body, signal) =>
        read(api.PUT('/api/v1/settings/pomodoro', { body, signal })),
      ),
      invalidatesTags: (_data, error) => (error ? [] : [{ type: 'Settings', id: 'pomodoro' }]),
    }),
    avatar: build.query<Model['AvatarSettings'], void>({
      queryFn: queryRequest((_, signal) => read(api.GET('/api/v1/settings/avatar', { signal }))),
      providesTags: [{ type: 'Settings', id: 'avatar' }],
    }),
    saveAvatar: build.mutation<Model['AvatarSettings'], Model['AvatarSettings']>({
      queryFn: mutationRequest((body, signal) =>
        read(api.PUT('/api/v1/settings/avatar', { body, signal })),
      ),
      invalidatesTags: (_data, error) => (error ? [] : [{ type: 'Settings', id: 'avatar' }]),
    }),
  }),
})

export const {
  useWorkProfilesQuery,
  useWorkProfilePickerInfiniteQuery,
  useSaveWorkProfileMutation,
  useDeleteWorkProfileMutation,
  usePrivacyQuery,
  useSaveConsentMutation,
  usePomodoroQuery,
  useSavePomodoroMutation,
  useAvatarQuery,
  useSaveAvatarMutation,
} = settingsApi
