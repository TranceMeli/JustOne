import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { flushPromises } from '@vue/test-utils'
import apiClient from '../../src/api/axios'
import { BACKEND_ORIGIN } from '../../src/config'
import { useAuthStore } from '../../src/stores/auth'
import { usePostCalendar } from '../../src/composables/usePostCalendar'

vi.mock('../../src/api/axios', () => ({ default: { get: vi.fn() } }))

// "Today" is Monday, 9 November 2026 (the 1st is a Sunday).
const TODAY = new Date(2026, 10, 9, 12, 0, 0)

function month(year, monthNumber, posts = {}) {
  const days = new Date(year, monthNumber, 0).getDate()
  return {
    data: {
      year,
      month: monthNumber,
      days: Array.from({ length: days }, (_, i) => {
        const p = posts[i + 1]
        return p
          ? { day: i + 1, hasPost: true, postId: p.id, imageUrl: p.imageUrl ?? null, text: p.text ?? null }
          : { day: i + 1, hasPost: false, postId: null, imageUrl: null, text: null }
      })
    }
  }
}

async function setup(response = month(2026, 11)) {
  apiClient.get.mockResolvedValue(response)
  const calendar = usePostCalendar()
  await flushPromises()
  return calendar
}

describe('usePostCalendar', () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(TODAY)
    setActivePinia(createPinia())
    useAuthStore().accessToken = 'token'
    apiClient.get.mockReset()
  })

  afterEach(() => vi.useRealTimers())

  it('loads the current month on start', async () => {
    await setup()

    expect(apiClient.get).toHaveBeenCalledWith('/posts/calendar', { params: { year: 2026, month: 11 } })
  })

  it('does not load anything while logged out', async () => {
    useAuthStore().accessToken = null
    await setup()

    expect(apiClient.get).not.toHaveBeenCalled()
  })

  it('maps days to posted, missed, today and future', async () => {
    const { monthDays } = await setup(month(2026, 11, {
      2: { id: 10, imageUrl: '/uploads/a.jpg' },
      3: { id: 11, text: 'A short poem' }
    }))
    const byDay = (d) => monthDays.value[d - 1]

    expect(byDay(1).status).toBe('missed')
    expect(byDay(2)).toMatchObject({ status: 'posted', kind: 'image', imageUrl: `${BACKEND_ORIGIN}/uploads/a.jpg`, postId: 10 })
    expect(byDay(3)).toMatchObject({ status: 'posted', kind: 'text', text: 'A short poem', postId: 11 })
    expect(byDay(9)).toMatchObject({ status: 'today', isToday: true })
    expect(byDay(10).status).toBe('future')
  })

  it('counts posted and elapsed days', async () => {
    const { postedCount, elapsedCount } = await setup(month(2026, 11, { 1: { id: 1, text: 'x' }, 5: { id: 2, text: 'y' } }))

    expect(postedCount.value).toBe(2)
    expect(elapsedCount.value).toBe(9)
  })

  it('offsets the grid so the 1st sits under the right weekday', async () => {
    const { leadingBlanks, prevMonth } = await setup()

    expect(leadingBlanks.value).toBe(6) // 1 Nov 2026 is a Sunday

    apiClient.get.mockResolvedValue(month(2026, 10))
    prevMonth()
    await flushPromises()

    expect(leadingBlanks.value).toBe(3) // 1 Oct 2026 is a Thursday
  })

  it('navigates back but never into the future', async () => {
    const { canGoNext, canGoPrev, nextMonth, prevMonth, goToCurrentMonth, viewMonth } = await setup()

    expect(canGoNext.value).toBe(false)
    nextMonth()
    expect(viewMonth.value).toBe(11)

    apiClient.get.mockResolvedValue(month(2026, 10))
    prevMonth()
    await flushPromises()
    expect(viewMonth.value).toBe(10)
    expect(canGoNext.value).toBe(true)
    expect(canGoPrev.value).toBe(true)
    expect(apiClient.get).toHaveBeenLastCalledWith('/posts/calendar', { params: { year: 2026, month: 10 } })

    apiClient.get.mockResolvedValue(month(2026, 11))
    goToCurrentMonth()
    await flushPromises()
    expect(viewMonth.value).toBe(11)
  })

  it('remembers today’s post and updates it after posting or deleting', async () => {
    const { todayPost, todayKnown, recordPost, removePost, monthDays } = await setup()

    expect(todayKnown.value).toBe(true)
    expect(todayPost.value).toBeNull()

    recordPost({ id: 99, imageUrl: null, text: 'Hello' })
    expect(todayPost.value).toEqual({ postId: 99, kind: 'text', text: 'Hello' })
    expect(monthDays.value[8]).toMatchObject({ status: 'posted', kind: 'text' })

    removePost()
    expect(todayPost.value).toBeNull()
    expect(monthDays.value[8].status).toBe('today')
  })

  it('shows an error and can retry', async () => {
    apiClient.get.mockRejectedValue(new Error('offline'))
    const calendar = usePostCalendar()
    await flushPromises()

    expect(calendar.errorMessage.value).toMatch(/not responding/)

    apiClient.get.mockResolvedValue(month(2026, 11))
    await calendar.retry()

    expect(calendar.errorMessage.value).toBe('')
    expect(calendar.monthDays.value).toHaveLength(30)
  })
})
