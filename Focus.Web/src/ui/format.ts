export function duration(seconds: number) {
  const minutes = Math.floor(Math.max(0, seconds) / 60)
  if (minutes >= 60) return Math.floor(minutes / 60) + 'h ' + (minutes % 60) + 'm'
  return minutes > 0 ? minutes + 'm' : Math.floor(Math.max(0, seconds)) + 's'
}

export function clockLabel(seconds: number) {
  const whole = Math.floor(Math.max(0, seconds))
  return String(Math.floor(whole / 60)).padStart(2, '0') + ':' + String(whole % 60).padStart(2, '0')
}

export function timestamp(value: string | null) {
  return value
    ? new Intl.DateTimeFormat('en', { dateStyle: 'medium', timeStyle: 'short' }).format(
        new Date(value),
      )
    : '—'
}

export function dayLabel(value: string) {
  return new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric' }).format(new Date(value))
}

export function rating(value: number | null) {
  return value == null ? '—' : value.toLocaleString('en', { maximumFractionDigits: 1 }) + ' / 5'
}

export function percent(value: number) {
  return Math.round(value) + '%'
}

export function calendarRange(from: string, through: string) {
  const start = parseLocalDay(from)
  const end = parseLocalDay(through)
  if ((from && !start) || (through && !end)) return null
  if (end) end.setDate(end.getDate() + 1)
  if (
    (start && start.getUTCFullYear() < 1) ||
    (end && end.getUTCFullYear() > 9999) ||
    (start && end && start >= end)
  )
    return null
  return { From: start?.toISOString(), To: end?.toISOString() }
}

function parseLocalDay(value: string) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value) || value.startsWith('0000')) return null
  const date = new Date(value + 'T00:00:00')
  return Number.isFinite(date.valueOf()) && localDay(date) === value ? date : null
}

export function localDay(value: Date) {
  return [
    String(value.getFullYear()).padStart(4, '0'),
    String(value.getMonth() + 1).padStart(2, '0'),
    String(value.getDate()).padStart(2, '0'),
  ].join('-')
}

export function recentRange(days: number) {
  const today = new Date()
  const start = new Date(today)
  start.setDate(start.getDate() - days + 1)
  return { from: localDay(start), through: localDay(today) }
}
