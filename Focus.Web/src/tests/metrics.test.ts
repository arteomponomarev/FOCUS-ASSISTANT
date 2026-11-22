import { expect, it } from 'vitest'
import { estimateDistribution } from '../features/analytics/data'
import { calendarRange, duration, rating } from '../ui/format'

it('keeps six estimated states and calculates duration ratios', () => {
  const values = estimateDistribution({ Focused: 30, Idle: 70 })
  expect(values).toHaveLength(6)
  expect(values.find((value) => value.state === 'Focused')).toEqual({
    state: 'Focused',
    seconds: 30,
    percent: 30,
  })
  expect(estimateDistribution({}).every((value) => value.percent === 0)).toBe(true)
})

it('renders missing ratings without inventing data and rejects reversed dates', () => {
  expect(rating(null)).toBe('—')
  expect(duration(3700)).toBe('1h 1m')
  expect(calendarRange('2026-11-25', '2026-11-22')).toBeNull()
  expect(calendarRange('2026-11-25', '2026-11-25')?.From).toBeTruthy()
})
