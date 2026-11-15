import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import apiClient from '../../src/api/axios'
import TodayCard from '../../src/components/TodayCard.vue'

vi.mock('../../src/api/axios', () => ({ default: { post: vi.fn(), put: vi.fn(), delete: vi.fn() } }))

const mountCard = (props = {}) =>
  mount(TodayCard, {
    props: { post: null, known: true, ...props },
    global: { stubs: { teleport: true } }
  })

// Simulates choosing a file in the hidden <input type="file">.
async function chooseFile(wrapper, file) {
  const input = wrapper.find('input[type="file"]')
  Object.defineProperty(input.element, 'files', { value: [file], configurable: true })
  await input.trigger('change')
}

describe('TodayCard', () => {
  beforeEach(() => {
    apiClient.post.mockReset()
    apiClient.put.mockReset()
    apiClient.delete.mockReset()
  })

  it('shows a skeleton until today’s status is known', () => {
    const wrapper = mountCard({ known: false })

    expect(wrapper.find('.photo-skeleton').exists()).toBe(true)
    expect(wrapper.find('.segmented').exists()).toBe(false)
  })

  it('starts with the photo/GIF drop zone', () => {
    const wrapper = mountCard()

    expect(wrapper.find('[role="radio"][aria-checked="true"]').text()).toContain('Photo / GIF')
    expect(wrapper.find('.drop').text()).toContain('JPG, PNG, GIF or WebP')
  })

  it('posts a text with line breaks', async () => {
    apiClient.post.mockResolvedValue({ data: { id: 5, imageUrl: null, text: 'Line one\nLine two' } })
    const wrapper = mountCard()

    await wrapper.findAll('[role="radio"]')[1].trigger('click')
    const textarea = wrapper.find('#today-text')
    await textarea.setValue('  Line one\nLine two  ')
    await wrapper.find('.btn-primary').trigger('click')
    await flushPromises()

    const form = apiClient.post.mock.calls[0][1]
    expect(apiClient.post.mock.calls[0][0]).toBe('/posts')
    expect(form.get('text')).toBe('Line one\nLine two')
    expect(form.get('file')).toBeNull()
    expect(wrapper.emitted('posted')[0][0]).toMatchObject({ id: 5, text: 'Line one\nLine two' })
  })

  it('counts down the characters and disables posting an empty text', async () => {
    const wrapper = mountCard()
    await wrapper.findAll('[role="radio"]')[1].trigger('click')

    expect(wrapper.find('#today-text-help').text()).toContain('500 characters left')
    expect(wrapper.find('.btn-primary').attributes('disabled')).toBeDefined()

    await wrapper.find('#today-text').setValue('Hello')

    expect(wrapper.find('#today-text-help').text()).toContain('495 characters left')
    expect(wrapper.find('.btn-primary').attributes('disabled')).toBeUndefined()
  })

  it('previews a chosen image and uploads it', async () => {
    apiClient.post.mockResolvedValue({ data: { id: 6, imageUrl: '/uploads/x.gif', text: null } })
    const wrapper = mountCard()

    await chooseFile(wrapper, new File(['gif'], 'fun.gif', { type: 'image/gif' }))
    expect(wrapper.find('img.photo').attributes('src')).toMatch(/^blob:/)

    await wrapper.find('.btn-primary').trigger('click')
    await flushPromises()

    expect(apiClient.post.mock.calls[0][1].get('file').name).toBe('fun.gif')
    expect(wrapper.emitted('posted')).toHaveLength(1)
  })

  it('rejects unsupported files and files over 10 MB before uploading', async () => {
    const wrapper = mountCard()

    await chooseFile(wrapper, new File(['x'], 'notes.pdf', { type: 'application/pdf' }))
    expect(wrapper.find('[role="alert"]').text()).toContain('not supported')

    const big = new File(['x'], 'huge.jpg', { type: 'image/jpeg' })
    Object.defineProperty(big, 'size', { value: 11 * 1024 * 1024 })
    await chooseFile(wrapper, big)
    expect(wrapper.find('[role="alert"]').text()).toContain('larger than 10 MB')

    expect(apiClient.post).not.toHaveBeenCalled()
  })

  it('explains a 409 conflict', async () => {
    apiClient.post.mockRejectedValue({ response: { status: 409 } })
    const wrapper = mountCard()
    await wrapper.findAll('[role="radio"]')[1].trigger('click')
    await wrapper.find('#today-text').setValue('Hi')

    await wrapper.find('.btn-primary').trigger('click')
    await flushPromises()

    expect(wrapper.find('[role="alert"]').text()).toContain('already posted today')
  })

  it('shows an existing text post and edits it with PUT', async () => {
    apiClient.put.mockResolvedValue({ data: { id: 7, imageUrl: null, text: 'Edited' } })
    const wrapper = mountCard({ post: { postId: 7, kind: 'text', text: 'Original' } })

    expect(wrapper.find('blockquote').text()).toBe('Original')
    expect(wrapper.find('.badge').text()).toContain('Posted')

    await wrapper.findAll('button').find((b) => b.text().includes('Change post')).trigger('click')
    expect(wrapper.find('#today-text').element.value).toBe('Original')

    await wrapper.find('#today-text').setValue('Edited')
    await wrapper.findAll('button').find((b) => b.text().includes('Save')).trigger('click')
    await flushPromises()

    expect(apiClient.put).toHaveBeenCalledTimes(1)
    expect(apiClient.put.mock.calls[0][1].get('text')).toBe('Edited')
  })

  it('asks before deleting and deletes after confirming', async () => {
    apiClient.delete.mockResolvedValue({})
    const wrapper = mountCard({ post: { postId: 8, kind: 'image', imageUrl: 'http://x/a.jpg' } })

    await wrapper.findAll('button').find((b) => b.text().includes('Delete')).trigger('click')
    const dialog = wrapper.find('[role="alertdialog"]')
    expect(dialog.text()).toContain('Delete today’s post?')
    expect(apiClient.delete).not.toHaveBeenCalled()

    await dialog.findAll('button').find((b) => b.text().includes('Delete post')).trigger('click')
    await flushPromises()

    expect(apiClient.delete).toHaveBeenCalledWith('/posts')
    expect(wrapper.emitted('deleted')).toHaveLength(1)
  })

  it('cancelling the dialog keeps the post', async () => {
    const wrapper = mountCard({ post: { postId: 8, kind: 'image', imageUrl: 'http://x/a.jpg' } })

    await wrapper.findAll('button').find((b) => b.text().includes('Delete')).trigger('click')
    await wrapper.find('[role="alertdialog"]').findAll('button').find((b) => b.text() === 'Cancel').trigger('click')

    expect(wrapper.find('[role="alertdialog"]').exists()).toBe(false)
    expect(apiClient.delete).not.toHaveBeenCalled()
  })
})
