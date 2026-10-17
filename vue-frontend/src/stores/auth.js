import { defineStore } from 'pinia'
import axios from 'axios'
import { BACKEND_ORIGIN } from '../config'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    accessToken: localStorage.getItem('accessToken') || null,
    accessExpiresAt: localStorage.getItem('accessExpiresAt') || null,
    refreshToken: localStorage.getItem('refreshToken') || null,
    refreshTokenExpiresAt: localStorage.getItem('refreshTokenExpiresAt') || null,
    email: localStorage.getItem('email') || null,
    roles: JSON.parse(localStorage.getItem('roles') || '[]')
  }),

  getters: {
    isLoggedIn: (state) => !!state.accessToken,
    isAdmin: (state) => state.roles.includes('Admin')
  },

  actions: {
    // Matches AuthResponse(userId, email, roles, accessToken, accessExpiresAt, refreshToken, refreshTokenExpiresAt)
    setSession(data) {
      this.accessToken = data.accessToken
      this.accessExpiresAt = data.accessExpiresAt
      this.refreshToken = data.refreshToken
      this.refreshTokenExpiresAt = data.refreshTokenExpiresAt
      this.email = data.email
      this.roles = data.roles

      localStorage.setItem('accessToken', data.accessToken)
      localStorage.setItem('accessExpiresAt', data.accessExpiresAt)
      localStorage.setItem('refreshToken', data.refreshToken)
      localStorage.setItem('refreshTokenExpiresAt', data.refreshTokenExpiresAt)
      localStorage.setItem('email', data.email)
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
    async refresh() {
      if (!this.refreshToken) {
        throw new Error('No refresh token available.')
      }

      const response = await axios.post(`${BACKEND_ORIGIN}/api/auth/refresh`, {
        refreshToken: this.refreshToken
      })
      this.setSession(response.data)
    },

    async logout() {
      const refreshToken = this.refreshToken

      this.accessToken = null
      this.accessExpiresAt = null
      this.refreshToken = null
      this.refreshTokenExpiresAt = null
      this.email = null
      this.roles = []

      localStorage.removeItem('accessToken')
      localStorage.removeItem('accessExpiresAt')
      localStorage.removeItem('refreshToken')
      localStorage.removeItem('refreshTokenExpiresAt')
      localStorage.removeItem('email')
      localStorage.removeItem('roles')

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