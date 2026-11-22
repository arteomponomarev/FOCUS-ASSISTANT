import { createSlice } from '@reduxjs/toolkit'
import type { PayloadAction } from '@reduxjs/toolkit'

export const authSlice = createSlice({
  name: 'auth',
  initialState: { signedIn: false },
  reducers: {
    sessionChanged: (_state, action: PayloadAction<boolean>) => ({ signedIn: action.payload }),
  },
})

export const { sessionChanged } = authSlice.actions
