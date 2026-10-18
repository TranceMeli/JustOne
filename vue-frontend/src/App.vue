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
import AppHeader from './components/AppHeader.vue'
import AppFooter from './components/AppFooter.vue'

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
  <AppHeader />

  <main>
    <template v-if="!authStore.isLoggedIn">
      <Login v-if="!showRegister" @switch-to-register="showRegister = true" />
      <Register v-else @switch-to-login="showRegister = false" />
    </template>

    <template v-else>
      <nav class="month-nav">
        <button :disabled="!canGoPrev" @click="prevMonth">&larr; Previous Month</button>
        <button :disabled="!canGoNext" @click="nextMonth">Next Month &rarr;</button>
      </nav>

      <p v-if="isLoading" class="status-text">Loading calendar...</p>
      <p v-else-if="errorMessage" class="status-text error">{{ errorMessage }}</p>

      <PostCalendar
        v-else
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

  <AppFooter />
</template>

<style scoped>
main {
  flex: 1;
  /* Side gutter on small screens; the extra right space leaves room for the card's offset shadow. */
  padding: 0 24px 0 16px;
}

.month-nav {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 16px;
  margin: 24px 0 16px;
}

.month-nav button {
  padding: 8px 16px;
  background: var(--color-dark);
  color: var(--color-light);
  border: 2px solid var(--color-dark);
  border-radius: var(--radius);
  cursor: pointer;
  font-weight: 600;
}

.month-nav button:not(:disabled):hover {
  background: var(--color-cyan);
  color: var(--color-dark);
}

.month-nav button:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

@media (max-width: 480px) {
  .month-nav {
    gap: 8px;
  }

  .month-nav button {
    padding: 8px 10px;
    font-size: 0.85rem;
  }
}

.status-text {
  text-align: center;
  color: var(--color-dark);
}

.status-text.error {
  color: var(--color-pink);
  font-weight: 600;
}
</style>