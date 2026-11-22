import { Moon, Sun } from 'lucide-react'
import { useEffect } from 'react'
import { useAppDispatch, useAppSelector } from './hooks'
import { themeKey, themeToggled } from './ui'

export function ThemeToggle() {
  const theme = useAppSelector((state) => state.ui.theme)
  const dispatch = useAppDispatch()
  useEffect(() => {
    document.documentElement.dataset.theme = theme
    try {
      localStorage.setItem(themeKey, theme)
    } catch {
      /* Theme persistence is optional. */
    }
  }, [theme])
  return (
    <button
      className="button ghost icon-button"
      type="button"
      aria-label={theme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'}
      onClick={() => dispatch(themeToggled())}
    >
      {theme === 'dark' ? (
        <Sun size={21} aria-hidden="true" />
      ) : (
        <Moon size={21} aria-hidden="true" />
      )}
    </button>
  )
}
