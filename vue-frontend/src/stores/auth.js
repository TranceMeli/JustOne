import { defineStore } from 'pinia'
import axios from 'axios'
import { BACKEND_ORIGIN } from '../config'

let refreshPromise = null

export const useAuthStore = defineStore('auth', {
  state: () => ({
    accessToken: localStorage.getItem('accessToken') || null,
    accessExpiresAt: localStorage.getItem('accessExpiresAt') || null,
    refreshToken: localStorage.getItem('refreshToken') || null,
    refreshTokenExpiresAt: localStorage.getItem('refreshTokenExpiresAt') || null,
    email: localStorage.getItem('email') || null,
    firstName: localStorage.getItem('firstName') || null,
    roles: JSON.parse(localStorage.getItem('roles') || '[]')
  }),

  getters: {
    isLoggedIn: (state) => !!state.accessToken,
    isAdmin: (state) => state.roles.includes('Admin'),
    // First name if the account has one, otherwise the part of the email before the @.
    displayName: (state) => state.firstName || state.email?.split('@')[0] || ''
  },

  actions: {
    // Matches AuthResponse(userId, email, firstName, roles, accessToken, accessExpiresAt, refreshToken, refreshTokenExpiresAt)
    setSession(data) {
      this.accessToken = data.accessToken
      this.accessExpiresAt = data.accessExpiresAt
      this.refreshToken = data.refreshToken
      this.refreshTokenExpiresAt = data.refreshTokenExpiresAt
      this.email = data.email
      this.firstName = data.firstName || null
      this.roles = data.roles

      localStorage.setItem('accessToken', data.accessToken)
      localStorage.setItem('accessExpiresAt', data.accessExpiresAt)
      localStorage.setItem('refreshToken', data.refreshToken)
      localStorage.setItem('refreshTokenExpiresAt', data.refreshTokenExpiresAt)
      localStorage.setItem('email', data.email)
      localStorage.setItem('firstName', data.firstName || '')
      localStorage.setItem('roles', JSON.stringify(data.roles))
    },

    async login(email, password) {
      // Use axios directly instead of apiClient: at login there's no token yet for the auth header.
      const response = await axios.post(`${BACKEND_ORIGIN}/api/auth/login`, { email, password })
      this.setSession(response.data)
    },

    // Registers a new user (always with the "User" role, enforced server-side)
    // and logs in right away so no second manual step is needed.
    async register(firstName, lastName, email, password) {
      await axios.post(`${BACKEND_ORIGIN}/api/auth/register`, {
        firstName,
        lastName,
        email,
        password
      })
      await this.login(email, password)
    },

    // Gets a new access token using the stored refresh token.
    // Also uses axios directly here: otherwise a failed refresh would trigger an
    // infinite loop in apiClient's 401 interceptor.
    // Concurrent callers share one in-flight request: sending the same refresh token twice
    // would make the backend treat the second call as token theft and revoke every session.
    refresh() {
      if (!this.refreshToken) {
        return Promise.reject(new Error('No refresh token available.'))
      }

      refreshPromise ??= axios
        .post(`${BACKEND_ORIGIN}/api/auth/refresh`, { refreshToken: this.refreshToken })
        .then((response) => this.setSession(response.data))
        .finally(() => {
          refreshPromise = null
        })

      return refreshPromise
    },

    async logout() {
      const refreshToken = this.refreshToken

      this.accessToken = null
      this.accessExpiresAt = null
      this.refreshToken = null
      this.refreshTokenExpiresAt = null
      this.email = null
      this.firstName = null
      this.roles = []

      for (const key of ['accessToken', 'accessExpiresAt', 'refreshToken', 'refreshTokenExpiresAt', 'email', 'firstName', 'roles']) {
        localStorage.removeItem(key)
      }

      // Best effort: tell the backend to invalidate the refresh token.
      // Logout should still succeed even if this call fails.
      if (refreshToken) {
        try {
          await axios.post(`${BACKEND_ORIGIN}/api/auth/revoke`, { refreshToken })
        } catch {
          // intentionally ignored
        }
      }
    }
  }
})