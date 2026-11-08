<script setup>
import { ref } from 'vue'
import { useAuthStore } from './stores/auth'
import { usePostCalendar } from './composables/usePostCalendar'
import AppHeader from './components/AppHeader.vue'
import AppFooter from './components/AppFooter.vue'
import Login from './components/Login.vue'
import Register from './components/Register.vue'
import PostCalendar from './components/PostCalendar.vue'
import TodayCard from './components/TodayCard.vue'
import Guestbook from './components/Guestbook.vue'
import ImageModal from './components/ImageModal.vue'
import ToastHost from './components/ToastHost.vue'

const authStore = useAuthStore()
const showRegister = ref(false)
const selectedDay = ref(null)
const todayCard = ref(null)

const calendar = usePostCalendar()
const {
  viewYear,
  viewMonth,
  isCurrentMonth,
  canGoPrev,
  canGoNext,
  leadingBlanks,
  monthDays,
  postedCount,
  elapsedCount,
  postedDays,
  todayPost,
  todayKnown,
  isLoading,
  errorMessage
} = calendar
</script>

<template>
  <AppHeader />

  <main>
    <template v-if="!authStore.isLoggedIn">
      <Login v-if="!showRegister" @switch-to-register="showRegister = true" />
      <Register v-else @switch-to-login="showRegister = false" />
    </template>

    <div v-else class="page">
      <div class="page-title">
        <h1>Hi {{ authStore.displayName }}</h1>
        <p v-if="todayKnown">{{ todayPost ? 'Done for today ✓' : 'You haven’t posted today yet.' }}</p>
      </div>

      <PostCalendar
        :year="viewYear"
        :month="viewMonth"
        :days="monthDays"
        :leading-blanks="leadingBlanks"
        :posted-count="postedCount"
        :elapsed-count="elapsedCount"
        :can-go-prev="canGoPrev"
        :can-go-next="canGoNext"
        :is-current-month="isCurrentMonth"
        :is-loading="isLoading"
        :error-message="errorMessage"
        @prev="calendar.prevMonth"
        @next="calendar.nextMonth"
        @current="calendar.goToCurrentMonth"
        @retry="calendar.retry"
        @select-day="selectedDay = $event"
        @add-today="todayCard?.openPicker()"
      />

      <div class="side">
        <TodayCard
          ref="todayCard"
          class="today"
          :post="todayPost"
          :known="todayKnown"
          @posted="calendar.recordPost"
          @deleted="calendar.removePost"
        />
        <Guestbook />
      </div>
    </div>

    <ImageModal
      :entry="selectedDay"
      :year="viewYear"
      :month="viewMonth"
      :posted-days="postedDays"
      @navigate="selectedDay = $event"
      @close="selectedDay = null"
    />
  </main>

  <AppFooter />
  <ToastHost />
</template>

<style scoped>
main {
  flex: 1;
}

.page {
  max-width: 1120px;
  margin: 0 auto;
  padding: var(--space-6) 24px 0;
  display: grid;
  grid-template-columns: minmax(0, 1fr) 340px;
  gap: var(--space-5);
  align-items: start;
}

.page-title {
  grid-column: 1 / -1;
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  justify-content: space-between;
  gap: var(--space-3);
}

.page-title h1 {
  margin: 0;
  font-size: var(--text-xl);
  letter-spacing: -0.01em;
}

.page-title p {
  margin: 0;
  color: var(--text-secondary);
  font-size: var(--text-sm);
}

.side {
  display: grid;
  gap: var(--space-5);
}

/* One column: today's task first, then the month, then the guestbook. */
@media (max-width: 900px) {
  .page {
    grid-template-columns: minmax(0, 1fr);
  }

  .side {
    display: contents;
  }

  .today {
    order: -1;
  }

  .page-title {
    order: -2;
  }
}

@media (max-width: 600px) {
  .page {
    padding: var(--space-4) 16px 0;
    gap: var(--space-4);
  }
}
</style>
