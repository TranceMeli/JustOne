import { ref, computed, watch } from 'vue'
import apiClient from '../api/axios'
import { BACKEND_ORIGIN } from '../config'
import { useAuthStore } from '../stores/auth'

// Read "today" fresh every time instead of once at module load, so a tab left open
// over midnight doesn't keep treating yesterday as today.
function currentDate() {
  const now = new Date()
  return { year: now.getFullYear(), month: now.getMonth() + 1, day: now.getDate() }
}

// A post is either an image (photo or GIF) or a text.
// Image paths from the backend are relative and get the backend origin prepended.
function toPost(postId, imageUrl, text, prefix = true) {
  if (imageUrl) {
    return { postId, kind: 'image', imageUrl: prefix ? `${BACKEND_ORIGIN}${imageUrl}` : imageUrl }
  }
  return { postId, kind: 'text', text }
}

export function usePostCalendar() {
  const authStore = useAuthStore()
  const start = currentDate()
  const viewYear = ref(start.year)
  const viewMonth = ref(start.month)
  const monthDays = ref([])
  const isLoading = ref(false)
  const errorMessage = ref('')

  // Today's post is kept separately, so the "Today" card stays correct while browsing older months.
  const todayPost = ref(null) // { postId, imageUrl } or { postId, text }, or null
  const todayKnown = ref(false)

  const isCurrentMonth = computed(() => {
    const today = currentDate()
    return viewYear.value === today.year && viewMonth.value === today.month
  })

  const canGoPrev = computed(() => !(viewYear.value === currentDate().year && viewMonth.value === 1))
  const canGoNext = computed(() => !isCurrentMonth.value)

  // Monday-based offset of the 1st, so the grid lines up under Mon..Sun.
  const leadingBlanks = computed(() => (new Date(viewYear.value, viewMonth.value - 1, 1).getDay() + 6) % 7)

  const postedCount = computed(() => monthDays.value.filter((d) => d.status === 'posted').length)
  const elapsedCount = computed(() => monthDays.value.filter((d) => d.status !== 'future').length)
  const postedDays = computed(() => monthDays.value.filter((d) => d.status === 'posted'))

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

  function goToCurrentMonth() {
    const today = currentDate()
    viewYear.value = today.year
    viewMonth.value = today.month
  }

  // Fetches the calendar data from the backend for the currently displayed month.
  async function fetchMonth() {
    // Without a token the request would fail anyway (401) - just wait until logged in.
    if (!authStore.isLoggedIn) {
      monthDays.value = []
      todayPost.value = null
      todayKnown.value = false
      return
    }

    isLoading.value = true
    errorMessage.value = ''

    try {
      const response = await apiClient.get('/posts/calendar', {
        params: { year: viewYear.value, month: viewMonth.value }
      })
      const today = currentDate()
      const current = isCurrentMonth.value

      monthDays.value = response.data.days.map((d) => {
        const base = { day: d.day, isToday: current && d.day === today.day }

        // Future days in the current month: not reached yet, regardless of the DB
        if (current && d.day > today.day) {
          return { ...base, status: 'future' }
        }
        if (d.hasPost) {
          return { ...base, status: 'posted', ...toPost(d.postId, d.imageUrl, d.text) }
        }
        return { ...base, status: base.isToday ? 'today' : 'missed' }
      })

      if (current) {
        const entry = monthDays.value.find((d) => d.isToday)
        todayPost.value = entry?.status === 'posted' ? toPost(entry.postId, entry.imageUrl, entry.text, false) : null
        todayKnown.value = true
      }
    } catch {
      errorMessage.value = 'The server is not responding. Check your connection and try again.'
    } finally {
      isLoading.value = false
    }
  }

  // Reload on every month navigation - and whenever the login status changes.
  watch([viewYear, viewMonth, () => authStore.isLoggedIn], fetchMonth, { immediate: true })

  function setTodayEntry(entry) {
    if (!isCurrentMonth.value) return
    const index = monthDays.value.findIndex((d) => d.isToday)
    if (index !== -1) {
      monthDays.value[index] = { ...monthDays.value[index], ...entry }
    }
  }

  // Optimistic update right after upload/change, without waiting for a re-fetch.
  // Takes the backend's PostResponse ({ id, imageUrl, text }).
  function recordPost(response) {
    const post = toPost(response.id, response.imageUrl, response.text)
    todayPost.value = post
    setTodayEntry({ status: 'posted', imageUrl: undefined, text: undefined, ...post })
  }

  // After deleting: today is open again.
  function removePost() {
    todayPost.value = null
    setTodayEntry({ status: 'today', imageUrl: undefined, text: undefined, postId: undefined })
  }

  return {
    viewYear,
    viewMonth,
    isCurrentMonth,
    canGoPrev,
    canGoNext,
    prevMonth,
    nextMonth,
    goToCurrentMonth,
    leadingBlanks,
    monthDays,
    postedCount,
    elapsedCount,
    postedDays,
    todayPost,
    todayKnown,
    recordPost,
    removePost,
    isLoading,
    errorMessage,
    retry: fetchMonth
  }
}
