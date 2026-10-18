import axios from 'axios'
import { useAuthStore } from '../stores/auth'
import { API_BASE_URL } from '../config'

const apiClient = axios.create({
  baseURL: API_BASE_URL
})

// Automatically attaches the current access token to the Authorization header on every request
apiClient.interceptors.request.use((config) => {
  const authStore = useAuthStore()
  if (authStore.accessToken) {
    config.headers.Authorization = `Bearer ${authStore.accessToken}`
  }
  // The backend decides what "today" is in the user's time zone, not in UTC.
  config.headers['X-Timezone'] = Intl.DateTimeFormat().resolvedOptions().timeZone
  return config
})

// On 401 (access token expired): try once to get a new access token via the
// refresh token and retry the original request.
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config
    const authStore = useAuthStore()

    if (error.response?.status === 401 && authStore.refreshToken && !originalRequest._retried) {
      originalRequest._retried = true // prevents an infinite loop if the refresh token is broken

      try {
        await authStore.refresh()
        originalRequest.headers.Authorization = `Bearer ${authStore.accessToken}`
        return apiClient(originalRequest)
      } catch {
        await authStore.logout()
      }
    } else if (error.response?.status === 401) {
      await authStore.logout()
    }

    return Promise.reject(error)
  }
)

export default apiClient