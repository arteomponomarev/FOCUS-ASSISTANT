import { Moon, Sun } from 'lucide-react'
import { useEffect, useState } from 'react'

type Theme = 'light' | 'dark'
const key = 'focus-assistant-theme'
function initialTheme(): Theme {
  try {
    const value = localStorage.getItem(key)
    if (value === 'dark' || value === 'light') return value
  } catch {
    /* Theme persistence is optional. */
  }
  return matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

export function ThemeToggle() {
  const [theme, setTheme] = useState<Theme>(initialTheme)
  useEffect(() => {
    document.documentElement.dataset.theme = theme
    try {
      localStorage.setItem(key, theme)
    } catch {
      /* Theme persistence is optional. */
    }
  }, [theme])
  return (
    <button
      className="button ghost icon-button"
      type="button"
      aria-label={theme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'}
      onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
    >
      {theme === 'dark' ? (
        <Sun size={21} aria-hidden="true" />
      ) : (
        <Moon size={21} aria-hidden="true" />
      )}
    </button>
  )
}
