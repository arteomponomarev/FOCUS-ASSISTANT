import { api, done, read } from '../../api/client'
import type { Model } from '../../api/client'

export const profiles = {
  list: (Page = 1, signal?: AbortSignal) =>
    read(
      api.GET('/api/v1/settings/work-profiles', {
        params: { query: { Page, PageSize: 25 } },
        signal,
      }),
    ),
  save: (body: Model['WorkProfileRequest'], id?: string) =>
    id
      ? read(api.PUT('/api/v1/settings/work-profiles/{id}', { params: { path: { id } }, body }))
      : read(api.POST('/api/v1/settings/work-profiles', { body })),
  remove: (id: string) =>
    done(api.DELETE('/api/v1/settings/work-profiles/{id}', { params: { path: { id } } })),
}
