import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import PostCalendar from '../../src/components/PostCalendar.vue'

const days = [
  { day: 1, status: 'missed' },
  { day: 2, status: 'posted', kind: 'image', imageUrl: 'http://x/a.jpg', postId: 10 },
  { day: 3, status: 'posted', kind: 'text', text: 'Morning fog on the river.', postId: 11 },
  { day: 4, status: 'today', isToday: true },
  { day: 5, status: 'future' }
]

function mountCalendar(props = {}) {
  return mount(PostCalendar, {
    props: {
      year: 2026,
      month: 11,
      days,
      leadingBlanks: 6,
      postedCount: 2,
      elapsedCount: 4,
      canGoPrev: true,
      canGoNext: false,
      isCurrentMonth: true,
      ...props
    }
  })
}

describe('PostCalendar', () => {
  it('shows the month, Monday-first weekdays and the progress', () => {
    const wrapper = mountCalendar()

    expect(wrapper.find('h2').text()).toBe('November 2026')
    expect(wrapper.findAll('.weekdays span').map((s) => s.text())).toEqual(['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'])
    expect(wrapper.find('.progress').text()).toContain('2 of 4 days posted')
  })

  it('adds blank cells before the 1st', () => {
    const wrapper = mountCalendar({ leadingBlanks: 3 })

    expect(wrapper.findAll('li.blank')).toHaveLength(3)
  })

  it('gives every day an accessible label and status', () => {
    const wrapper = mountCalendar()
    const labels = wrapper.findAll('.day').map((d) => d.attributes('aria-label'))

    expect(labels).toEqual([
      '1 November – missed',
      'Photo from 2 November',
      'Text from 3 November',
      'Today, 4 November – add a post',
      '5 November'
    ])
    // Missed days have an icon and the word, not only a colour.
    expect(wrapper.find('.day.missed').text()).toContain('missed')
  })

  it('makes only posted days and today clickable', () => {
    const wrapper = mountCalendar()

    expect(wrapper.findAll('button.day')).toHaveLength(3)
    expect(wrapper.find('.day.missed').element.tagName).toBe('DIV')
  })

  it('shows the beginning of a text post on its tile', () => {
    const wrapper = mountCalendar()

    expect(wrapper.find('.day.text .excerpt').text()).toContain('Morning fog')
  })

  it('emits select-day for a post and add-today for today', async () => {
    const wrapper = mountCalendar()

    await wrapper.find('.day.posted.image').trigger('click')
    await wrapper.find('.day.today').trigger('click')

    expect(wrapper.emitted('select-day')[0][0]).toMatchObject({ day: 2, postId: 10 })
    expect(wrapper.emitted('add-today')).toHaveLength(1)
  })

  it('disables "next month" and "Today" in the current month', () => {
    const wrapper = mountCalendar()

    expect(wrapper.find('[aria-label="Next month"]').attributes('disabled')).toBeDefined()
    expect(wrapper.find('.today-button').attributes('disabled')).toBeDefined()
    expect(wrapper.find('[aria-label="Previous month"]').attributes('disabled')).toBeUndefined()
  })

  it('emits navigation events', async () => {
    const wrapper = mountCalendar({ canGoNext: true, isCurrentMonth: false })

    await wrapper.find('[aria-label="Previous month"]').trigger('click')
    await wrapper.find('[aria-label="Next month"]').trigger('click')
    await wrapper.find('.today-button').trigger('click')

    expect(wrapper.emitted('prev')).toHaveLength(1)
    expect(wrapper.emitted('next')).toHaveLength(1)
    expect(wrapper.emitted('current')).toHaveLength(1)
  })

  it('shows skeleton tiles while loading', () => {
    const wrapper = mountCalendar({ isLoading: true })

    expect(wrapper.findAll('.day-skeleton')).toHaveLength(35)
    expect(wrapper.find('.progress').exists()).toBe(false)
  })

  it('shows the error with a retry button', async () => {
    const wrapper = mountCalendar({ errorMessage: 'The server is not responding.' })

    expect(wrapper.find('[role="alert"]').text()).toContain('Calendar could not be loaded.')
    await wrapper.find('.retry button').trigger('click')

    expect(wrapper.emitted('retry')).toHaveLength(1)
  })
})
