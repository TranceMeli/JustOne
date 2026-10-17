<script setup>
import { computed, ref } from 'vue'
import { useAuthStore } from './stores/auth'
import { usePostCalendar } from './composables/usePostCalendar'
import Login from './components/Login.vue'
import Register from './components/Register.vue'
import PostCalendar from './components/PostCalendar.vue'
import PostUpload from './components/PostUpload.vue'
import ImageModal from './components/ImageModal.vue'
import Guestbook from './components/Guestbook.vue'

const authStore = useAuthStore()
const selectedImage = ref(null)
const showRegister = ref(false)

const {
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
} = usePostCalendar()

const monthNames = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December'
]

const monthLabel = computed(() =>
  `${monthNames[viewMonth.value - 1]} ${viewYear.value}`.toUpperCase()
)

function onPosted(imageUrl, postId) {
  recordPost(imageUrl, postId)
}

function onDeleted() {
  removePost()
}
</script>

<template>
  <main>
    <template v-if="!authStore.isLoggedIn">
      <Login v-if="!showRegister" @switch-to-register="showRegister = true" />
      <Register v-else @switch-to-login="showRegister = false" />
    </template>

    <template v-else>
      <nav class="month-nav">
        <button :disabled="!canGoPrev" @click="prevMonth">&larr; Previous Month</button>
        <button :disabled="!canGoNext" @click="nextMonth">Next Month &rarr;</button>
        <button type="button" @click="authStore.logout()">Log out</button>
      </nav>

      <p v-if="isLoading" class="status-text">Loading calendar...</p>
      <p v-else-if="errorMessage" class="status-text error">{{ errorMessage }}</p>

      <PostCalendar
        v-else
        :username="authStore.email"
        :month-label="monthLabel"
        :days="monthDays"
        @select-image="selectedImage = $event"
      />

      <PostUpload
        v-if="isCurrentMonth"
        :existing-image-url="todayImageUrl"
        @posted="onPosted"
        @deleted="onDeleted"
      />

      <Guestbook />
    </template>

    <ImageModal
      :image-url="selectedImage?.imageUrl"
      :post-id="selectedImage?.postId"
      @close="selectedImage = null"
    />
  </main>
</template>

<style scoped>
.month-nav {
  display: flex;
  justify-content: center;
  gap: 16px;
  margin: 16px 0;
  font-family: system-ui, sans-serif;
}

.month-nav button {
  padding: 8px 16px;
  background: #252a34;
  color: #eaeaea;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-weight: 600;
}

.month-nav button:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.status-text {
  text-align: center;
  font-family: system-ui, sans-serif;
  color: #252a34;
}

.status-text.error {
  color: #ff2e63;
}
</style>