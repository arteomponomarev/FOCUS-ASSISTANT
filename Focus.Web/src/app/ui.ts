import { createSlice } from '@reduxjs/toolkit'

type Theme = 'light' | 'dark'
export const themeKey = 'focus-assistant-theme'

function initialTheme(): { theme: Theme } {
  try {
    const value = localStorage.getItem(themeKey)
    if (value === 'dark' || value === 'light') return { theme: value }
  } catch {
    /* Theme persistence is optional. */
  }
  return {
    theme: globalThis.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light',
  }
}

export const uiSlice = createSlice({
  name: 'ui',
  initialState: initialTheme,
  reducers: {
    themeToggled(state) {
      state.theme = state.theme === 'dark' ? 'light' : 'dark'
    },
  },
})

export const { themeToggled } = uiSlice.actions
