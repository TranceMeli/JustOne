import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import apiClient from '../../src/api/axios'
import Comments from '../../src/components/Comments.vue'
import Guestbook from '../../src/components/Guestbook.vue'

vi.mock('../../src/api/axios', () => ({ default: { get: vi.fn(), post: vi.fn() } }))

const comments = [
  { id: 1, message: 'Love the colours', createdAt: '2026-11-08T10:00:00Z', authorName: 'Lena H.', isOwn: false },
  { id: 2, message: 'Thanks!', createdAt: '2026-11-08T11:00:00Z', authorName: 'Anna B.', isOwn: true }
]

describe('Comments', () => {
  beforeEach(() => {
    apiClient.get.mockReset()
    apiClient.post.mockReset()
  })

  async function mountComments(list = comments) {
    apiClient.get.mockResolvedValue({ data: list })
    const wrapper = mount(Comments, { props: { postId: 42 } })
    await flushPromises()
    return wrapper
  }

  it('loads the comments of the post with names and a "You" badge', async () => {
    const wrapper = await mountComments()

    expect(apiClient.get).toHaveBeenCalledWith('/posts/42/comments')
    expect(wrapper.findAll('.comment')).toHaveLength(2)
    expect(wrapper.text()).toContain('Lena H.')
    expect(wrapper.findAll('.you')).toHaveLength(1)
    expect(wrapper.find('.count').text()).toBe('2')
  })

  it('shows an empty state without comments', async () => {
    const wrapper = await mountComments([])

    expect(wrapper.find('.empty').text()).toContain('No comments yet')
  })

  it('sends with Enter and clears the field', async () => {
    apiClient.post.mockResolvedValue({ data: { id: 3, message: 'Nice', createdAt: '2026-11-09T09:00:00Z', authorName: 'Anna B.', isOwn: true } })
    const wrapper = await mountComments()
    const textarea = wrapper.find('textarea')

    await textarea.setValue(' Nice ')
    await textarea.trigger('keydown', { key: 'Enter' })
    await flushPromises()

    expect(apiClient.post).toHaveBeenCalledWith('/posts/42/comments', { message: 'Nice' })
    expect(wrapper.findAll('.comment')).toHaveLength(3)
    expect(textarea.element.value).toBe('')
  })

  it('does not send with Shift+Enter (new line instead)', async () => {
    const wrapper = await mountComments()
    const textarea = wrapper.find('textarea')

    await textarea.setValue('First line')
    await textarea.trigger('keydown', { key: 'Enter', shiftKey: true })

    expect(apiClient.post).not.toHaveBeenCalled()
  })

  it('keeps the text and shows an error when sending fails', async () => {
    apiClient.post.mockRejectedValue(new Error('offline'))
    const wrapper = await mountComments()

    await wrapper.find('textarea').setValue('Please keep me')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.find('[role="alert"]').text()).toContain('could not be sent')
    expect(wrapper.find('textarea').element.value).toBe('Please keep me')
  })

  it('inserts emojis from the picker', async () => {
    const wrapper = await mountComments()

    await wrapper.find('.emoji-toggle').trigger('click')
    await wrapper.find('.emoji-button').trigger('click')

    expect(wrapper.find('textarea').element.value).toBe('😀')
  })
})

describe('Guestbook', () => {
  const entries = [
    { id: 1, message: 'Hello everyone!', createdAt: '2026-11-09T08:00:00Z', authorName: 'Max W.', isOwn: false },
    { id: 2, message: 'Welcome!', createdAt: '2026-11-08T08:00:00Z', authorName: 'Anna B.', isOwn: true }
  ]

  beforeEach(() => {
    apiClient.get.mockReset()
    apiClient.post.mockReset()
  })

  it('shows everyone’s entries and says that it is public', async () => {
    apiClient.get.mockResolvedValue({ data: entries })
    const wrapper = mount(Guestbook)
    await flushPromises()

    expect(wrapper.text()).toContain('Visible to everyone')
    expect(wrapper.findAll('.entry')).toHaveLength(2)
    expect(wrapper.find('.entry.own').text()).toContain('You')
    expect(wrapper.text()).toContain('Max W.')
  })

  it('adds a new entry on top', async () => {
    apiClient.get.mockResolvedValue({ data: entries })
    apiClient.post.mockResolvedValue({ data: { id: 3, message: 'New!', createdAt: '2026-11-09T09:00:00Z', authorName: 'Anna B.', isOwn: true } })
    const wrapper = mount(Guestbook)
    await flushPromises()

    await wrapper.find('textarea').setValue('New!')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(apiClient.post).toHaveBeenCalledWith('/guestbook', { message: 'New!' })
    expect(wrapper.find('.entry p').text()).toBe('New!')
  })

  it('shows an empty state', async () => {
    apiClient.get.mockResolvedValue({ data: [] })
    const wrapper = mount(Guestbook)
    await flushPromises()

    expect(wrapper.find('.empty').text()).toContain('No entries yet')
  })
})
