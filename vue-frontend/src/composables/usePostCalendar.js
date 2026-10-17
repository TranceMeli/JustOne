import { ref, computed, watch } from 'vue'
import apiClient from '../api/axios'
import { BACKEND_ORIGIN } from '../config'
import { useAuthStore } from '../stores/auth'

const today = new Date()
const CURRENT_YEAR = today.getFullYear()
const CURRENT_MONTH = today.getMonth() + 1 // 1-12
const CURRENT_DAY = today.getDate()

export function usePostCalendar() {
  const authStore = useAuthStore()
  const viewYear = ref(CURRENT_YEAR)
  const viewMonth = ref(CURRENT_MONTH)
  const monthDays = ref([])
  const isLoading = ref(false)
  const errorMessage = ref('')

  const isCurrentMonth = computed(
    () => viewYear.value === CURRENT_YEAR && viewMonth.value === CURRENT_MONTH
  )

  const canGoPrev = computed(() => !(viewYear.value === CURRENT_YEAR && viewMonth.value === 1))
  const canGoNext = computed(() => !isCurrentMonth.value)

  function prevMonth() {
    if (!canGoPrev.value) return
    if (viewMonth.value === 1) {
      viewMonth.value = 12
      viewYear.value -= 1
    } else {
      viewMonth.value -= 1
    }
  }

  function nextMonth() {
    if (!canGoNext.value) return
    if (viewMonth.value === 12) {
      viewMonth.value = 1
      viewYear.value += 1
    } else {
      viewMonth.value += 1
    }
  }

  // Fetches the real calendar data from the backend for the currently displayed month.
  async function fetchMonth() {
    // Without a token the request would fail anyway (401) - just wait until logged in
    // instead of showing an error message.
    if (!authStore.isLoggedIn) {
      return
    }

    isLoading.value = true
    errorMessage.value = ''

    try {
      const response = await apiClient.get('/posts/calendar', {
        params: { year: viewYear.value, month: viewMonth.value }
      })

      monthDays.value = response.data.days.map((d) => {
        // Future days in the current month: not reached yet, regardless of the DB
        if (isCurrentMonth.value && d.day > CURRENT_DAY) {
          return { day: d.day, status: 'future' }
        }

        // Today, but not posted yet
        if (isCurrentMonth.value && d.day === CURRENT_DAY && !d.hasPost) {
          return { day: d.day, status: 'today' }
        }

        return {
          day: d.day,
          status: d.hasPost ? 'posted' : 'missed',
          imageUrl: d.imageUrl ? `${BACKEND_ORIGIN}${d.imageUrl}` : null,
          postId: d.postId ?? null
        }
      })
    } catch (err) {
      errorMessage.value = 'Could not load the calendar.'
    } finally {
      isLoading.value = false
    }
  }

  // Reload on every month navigation - and also whenever the login status changes
  // (e.g. right after login, when no token existed yet before).
  watch([viewYear, viewMonth, () => authStore.isLoggedIn], fetchMonth, { immediate: true })

  const hasPostedToday = computed(() => {
    if (!isCurrentMonth.value) return false
    const entry = monthDays.value.find((d) => d.day === CURRENT_DAY)
    return entry?.status === 'posted'
  })

  const todayImageUrl = computed(() => {
    if (!isCurrentMonth.value) return null
    const entry = monthDays.value.find((d) => d.day === CURRENT_DAY)
    return entry?.imageUrl ?? null
  })

  // Optimistic update right after upload/change, without waiting for a re-fetch.
  function recordPost(imageUrl, postId) {
    const index = monthDays.value.findIndex((d) => d.day === CURRENT_DAY)
    if (index !== -1) {
      monthDays.value[index] = { day: CURRENT_DAY, status: 'posted', imageUrl, postId }
    }
  }

  // After deleting: reset today back to "still open".
  function removePost() {
    const index = monthDays.value.findIndex((d) => d.day === CURRENT_DAY)
    if (index !== -1) {
      monthDays.value[index] = { day: CURRENT_DAY, status: 'today' }
    }
  }

  return {
    viewYear,
    viewMonth,
    isCurrentMonth,
    canGoPrev,
    canGoNext,
    prevMonth,
    nextMonth,
    monthDays,
    hasPostedToday,
    todayImageUrl,
    recordPost,
    removePost,
    isLoading,
    errorMessage
  }
}