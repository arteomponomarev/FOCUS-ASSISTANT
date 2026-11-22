import { useRef, useState } from 'react'

export function usePrivateAction<T>(run: (arg: T) => Promise<void>, onSuccess: () => void) {
  const busy = useRef(false)
  const [isLoading, setLoading] = useState(false)
  const [error, setError] = useState<unknown>()

  async function submit(arg: T) {
    if (busy.current) return
    busy.current = true
    setLoading(true)
    setError(undefined)
    try {
      await run(arg)
      onSuccess()
    } catch (failure) {
      setError(failure)
    } finally {
      busy.current = false
      setLoading(false)
    }
  }

  return { submit, isLoading, error, reset: () => setError(undefined) }
}
