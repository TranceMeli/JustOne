import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import apiClient from '../../src/api/axios'
import { useAuthStore } from '../../src/stores/auth'
import ImageModal from '../../src/components/ImageModal.vue'
import Login from '../../src/components/Login.vue'

vi.mock('../../src/api/axios', () => ({ default: { get: vi.fn(), post: vi.fn() } }))

const posted = [
  { day: 2, kind: 'image', imageUrl: 'http://x/a.jpg', postId: 10 },
  { day: 3, kind: 'text', text: 'Morning fog.\nOnly the bridge and me.', postId: 11 },
  { day: 5, kind: 'image', imageUrl: 'http://x/b.gif', postId: 12 }
]

function mountModal(entry) {
  return mount(ImageModal, {
    props: { entry, year: 2026, month: 11, postedDays: posted },
    global: { stubs: { teleport: true } },
    attachTo: document.body
  })
}

describe('ImageModal', () => {
  beforeEach(() => apiClient.get.mockResolvedValue({ data: [] }))

  it('shows the date as title and the image of an image post', async () => {
    const wrapper = mountModal(posted[0])
    await flushPromises()

    expect(wrapper.find('#modal-title').text()).toBe('Monday 2 November')
    expect(wrapper.find('.photo-area img').attributes('src')).toBe('http://x/a.jpg')
    wrapper.unmount()
  })

  it('shows the text of a text post with its line breaks', async () => {
    const wrapper = mountModal(posted[1])
    await flushPromises()

    expect(wrapper.find('.photo-area').exists()).toBe(false)
    expect(wrapper.find('.text-area blockquote').text()).toBe('Morning fog.\nOnly the bridge and me.')
    wrapper.unmount()
  })

  it('navigates with the arrow keys and closes with Escape', async () => {
    const wrapper = mountModal(null)
    await wrapper.setProps({ entry: posted[1] }) // opening registers the key handler
    await flushPromises()

    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft' }))
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight' }))
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))

    expect(wrapper.emitted('navigate').map(([e]) => e.day)).toEqual([2, 5])
    expect(wrapper.emitted('close')).toHaveLength(1)
    wrapper.unmount()
  })

  it('disables "previous" on the first post of the month', async () => {
    const wrapper = mountModal(posted[0])
    await flushPromises()

    expect(wrapper.find('[aria-label="Previous post"]').attributes('disabled')).toBeDefined()
    expect(wrapper.find('[aria-label="Next post"]').attributes('disabled')).toBeUndefined()
    wrapper.unmount()
  })
})

describe('Login', () => {
  beforeEach(() => setActivePinia(createPinia()))

  it('has visible labels for both fields', () => {
    const wrapper = mount(Login)

    expect(wrapper.find('label[for="login-email"]').text()).toBe('Email')
    expect(wrapper.find('label[for="login-password"]').text()).toBe('Password')
  })

  it('logs in with the entered credentials', async () => {
    const store = useAuthStore()
    const login = vi.spyOn(store, 'login').mockResolvedValue()
    const wrapper = mount(Login)

    await wrapper.find('#login-email').setValue('me@test.local')
    await wrapper.find('#login-password').setValue('Secret1!')
    await wrapper.find('form').trigger('submit')

    expect(login).toHaveBeenCalledWith('me@test.local', 'Secret1!')
  })

  it('logs in as the demo user with one click', async () => {
    const store = useAuthStore()
    const login = vi.spyOn(store, 'login').mockResolvedValue()
    const wrapper = mount(Login)

    await wrapper.findAll('button').find((b) => b.text().includes('demo')).trigger('click')

    expect(login).toHaveBeenCalledWith('anna@justone.com', 'Demo123!')
  })

  it('explains a wrong password', async () => {
    const store = useAuthStore()
    vi.spyOn(store, 'login').mockRejectedValue({ response: { status: 401 } })
    const wrapper = mount(Login)

    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(wrapper.find('[role="alert"]').text()).toContain('Email or password is wrong')
  })

  it('switches to the registration', async () => {
    const wrapper = mount(Login)

    await wrapper.find('a').trigger('click')

    expect(wrapper.emitted('switch-to-register')).toHaveLength(1)
  })
})
