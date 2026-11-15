import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import axios from 'axios'
import { useAuthStore } from '../../src/stores/auth'

vi.mock('axios', () => ({ default: { post: vi.fn() } }))

const session = {
  userId: 'u1',
  email: 'anna@justone.com',
  firstName: 'Anna',
  roles: ['User'],
  accessToken: 'access-1',
  accessExpiresAt: '2026-11-15T12:00:00Z',
  refreshToken: 'refresh-1',
  refreshTokenExpiresAt: '2026-11-22T12:00:00Z'
}

describe('auth store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    axios.post.mockReset()
  })

  it('stores the session and keeps it in localStorage', async () => {
    axios.post.mockResolvedValue({ data: session })
    const store = useAuthStore()

    await store.login('anna@justone.com', 'Demo123!')

    expect(axios.post).toHaveBeenCalledWith(expect.stringContaining('/api/auth/login'), {
      email: 'anna@justone.com',
      password: 'Demo123!'
    })
    expect(store.isLoggedIn).toBe(true)
    expect(store.displayName).toBe('Anna')
    expect(localStorage.getItem('accessToken')).toBe('access-1')
    expect(JSON.parse(localStorage.getItem('roles'))).toEqual(['User'])
  })

  it('uses the email name when there is no first name', () => {
    const store = useAuthStore()
    store.setSession({ ...session, firstName: '' })

    expect(store.displayName).toBe('anna')
  })

  it('knows admins by their role', () => {
    const store = useAuthStore()
    store.setSession({ ...session, roles: ['Admin'] })

    expect(store.isAdmin).toBe(true)
  })

  it('sends only one refresh request when several callers refresh at once', async () => {
    // Two parallel refreshes with the same token would look like token theft to the backend.
    let resolve
    axios.post.mockReturnValue(new Promise((r) => (resolve = r)))
    const store = useAuthStore()
    store.setSession(session)

    const first = store.refresh()
    const second = store.refresh()
    resolve({ data: { ...session, accessToken: 'access-2', refreshToken: 'refresh-2' } })
    await Promise.all([first, second])

    expect(axios.post).toHaveBeenCalledTimes(1)
    expect(store.accessToken).toBe('access-2')
  })

  it('rejects a refresh without a refresh token', async () => {
    const store = useAuthStore()

    await expect(store.refresh()).rejects.toThrow('No refresh token')
    expect(axios.post).not.toHaveBeenCalled()
  })

  it('clears everything on logout and revokes the refresh token', async () => {
    axios.post.mockResolvedValue({})
    const store = useAuthStore()
    store.setSession(session)

    await store.logout()

    expect(store.isLoggedIn).toBe(false)
    expect(store.email).toBeNull()
    expect(localStorage.getItem('accessToken')).toBeNull()
    expect(axios.post).toHaveBeenCalledWith(expect.stringContaining('/api/auth/revoke'), { refreshToken: 'refresh-1' })
  })

  it('still logs out when the revoke call fails', async () => {
    axios.post.mockRejectedValue(new Error('offline'))
    const store = useAuthStore()
    store.setSession(session)

    await store.logout()

    expect(store.isLoggedIn).toBe(false)
  })
})
