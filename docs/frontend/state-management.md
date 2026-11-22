# Frontend state ownership

| State | Owner | Lifetime |
| --- | --- | --- |
| API records and read errors | RTK Query | Short-lived cache, scoped to current browser identity |
| Signed-in state | Redux Toolkit auth slice | Reset when the transport replaces or clears credentials |
| Session credentials | HTTP transport | Memory only, until logout, revocation or page reload |
| Account export progress | Redux Toolkit slice and `createAsyncThunk` | Pending, succeeded or failed while the Privacy view is mounted. Export contents go directly to the downloaded file |
| Filters and pagination | URL search parameters for history, page state elsewhere | Shareable URLs for history, mounted route for other filters |
| Inputs and validation | React Hook Form or native forms | Until submission or unmount |
| Modal visibility and running timer | Feature component | Local interaction only |
| Theme | Redux Toolkit UI slice and one localStorage preference | Persists across reloads |

RTK Query cache keys include endpoint arguments such as route IDs, dates and pagination. Session and goal mutations invalidate the affected detail and signals as well as dependent lists and analytics. Work profile changes refresh both the settings list and the session picker.

When credentials change, the auth bridge aborts active requests and resets the RTK Query cache. Password-bearing forms call the HTTP client directly. Passwords and tokens stay outside Redux actions, store snapshots and browser storage.

The signals saga dispatches invalidations every two seconds for an open active or paused session. Completed sessions continue refreshing until two minutes after the server's completion timestamp. Closing the view or changing authentication stops the saga. RTK Query updates active subscriptions for the selected tab and privacy preference.
