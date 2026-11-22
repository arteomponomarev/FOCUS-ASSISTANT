import { api, read } from '../../api/client'
import type { Model } from '../../api/client'
import { focusApi, mutationRequest, queryRequest } from '../../api/state'

const profileApi = focusApi.injectEndpoints({
  endpoints: (build) => ({
    profile: build.query<Model['Profile'], void>({
      queryFn: queryRequest((_, signal) => read(api.GET('/api/v1/auth/me', { signal }))),
      providesTags: ['Profile'],
    }),
    renameProfile: build.mutation<Model['Profile'], string>({
      queryFn: mutationRequest((displayName, signal) =>
        read(api.PATCH('/api/v1/auth/me', { body: { displayName }, signal })),
      ),
      invalidatesTags: (_data, error) => (error ? [] : ['Profile']),
    }),
  }),
})

export const { useProfileQuery, useRenameProfileMutation } = profileApi
