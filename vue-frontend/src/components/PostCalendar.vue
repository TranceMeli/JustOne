<script setup>
import { computed } from 'vue'

const props = defineProps({
  monthLabel: { type: String, required: true },
  days: { type: Array, required: true }
})

// Days that have already started (everything except the future), and how many got a photo.
const postedCount = computed(() => props.days.filter((d) => d.status === 'posted').length)
const elapsedCount = computed(() => props.days.filter((d) => d.status !== 'future').length)

const emit = defineEmits(['select-image'])

function onSquareClick(day) {
  if (day.imageUrl) {
    emit('select-image', { imageUrl: day.imageUrl, postId: day.postId })
  }
}
</script>

<template>
  <div class="calendar-card">
    <header class="calendar-header">
      <span class="app-name">My Calendar</span>
      <span class="stats">
        <strong>{{ postedCount }}</strong> / {{ elapsedCount }} days posted
      </span>
    </header>

    <h2 class="calendar-title">{{ monthLabel }}</h2>

    <div class="calendar-grid">
      <div
        v-for="d in days"
        :key="d.day"
        class="day-square"
        :class="[d.status, { clickable: d.imageUrl }]"
        @click="onSquareClick(d)"
      >
        <div class="square-bg" />
        <img v-if="d.imageUrl" :src="d.imageUrl" :alt="`Post from the ${d.day}.`" />
        <span v-else-if="d.status === 'today'" class="today-label">Today</span>
        <span v-else-if="d.status !== 'future'" class="day-number">
          {{ String(d.day).padStart(2, '0') }}
        </span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.calendar-card {
  max-width: 760px;
  margin: 0 auto;
  background: var(--color-light);
  border: 2px solid var(--color-dark);
  border-radius: var(--radius);
  box-shadow: 8px 8px 0 var(--color-pink);
  overflow: hidden;
}

.calendar-header {
  background: var(--color-cyan);
  color: var(--color-dark);
  border-bottom: 4px solid var(--color-dark);
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 24px;
  font-weight: 700;
  font-size: 1.1rem;
}

.app-name {
  font-size: 1.3rem;
  font-weight: 900;
  letter-spacing: 1px;
}

.calendar-title {
  text-align: center;
  font-size: 1.8rem;
  font-weight: 800;
  color: var(--color-dark);
  letter-spacing: 1px;
  margin: 28px 0;
}

.calendar-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(90px, 1fr));
  gap: 14px;
  padding: 0 24px 28px;
}

.stats {
  padding: 4px 12px;
  background: var(--color-dark);
  color: var(--color-light);
  border-radius: 999px;
  font-size: 0.9rem;
  white-space: nowrap;
}

.stats strong {
  color: var(--color-cyan);
}

@media (max-width: 480px) {
  .calendar-header {
    flex-wrap: wrap;
    gap: 8px;
    padding: 12px 16px;
    font-size: 0.95rem;
  }

  .stats {
    font-size: 0.8rem;
  }


  .calendar-title {
    font-size: 1.4rem;
    margin: 20px 0;
  }

  .calendar-grid {
    grid-template-columns: repeat(auto-fill, minmax(56px, 1fr));
    gap: 8px;
    padding: 0 12px 16px;
  }

  .day-number {
    font-size: 1.1rem;
  }

  .today-label {
    font-size: 0.7rem;
  }
}

.day-square {
  position: relative;
  aspect-ratio: 1 / 1;
  border-radius: var(--radius);
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  transition: transform var(--transition), box-shadow var(--transition);
}

/* Separate layer for the SVG graphic. The SVG itself has no color (only brightness/shape) -
   the actual color comes from background-color and is clipped via a CSS mask. */
.square-bg {
  position: absolute;
  inset: 0;
  z-index: 0;
  background-color: var(--color-cyan);
  transition: background-color var(--transition);
  -webkit-mask-image: url('/day-square.svg');
  mask-image: url('/day-square.svg');
  -webkit-mask-size: cover;
  mask-size: cover;
  -webkit-mask-position: center;
  mask-position: center;
}

.day-square img {
  position: relative;
  z-index: 1;
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.day-square.clickable {
  cursor: pointer;
}

.day-square.clickable:hover {
  transform: translateY(-3px);
  box-shadow: 0 0 0 3px var(--color-cyan), 5px 5px 0 3px var(--color-dark);
}

.day-square.missed .square-bg {
  background-color: var(--color-pink);
}

.day-square.missed:hover .square-bg {
  background-color: var(--color-dark);
}

.day-square.missed:hover .day-number {
  color: var(--color-pink);
  text-shadow: none;
}

.day-square.future .square-bg,
.day-square.today .square-bg {
  display: none;
}

.day-square.future {
  border: 1px dashed var(--color-muted);
  opacity: 0.5;
}

.day-square.today {
  background: var(--color-cyan-soft);
  border: 2px dashed var(--color-cyan);
  animation: today-pulse 2s ease-in-out infinite;
}

@keyframes today-pulse {
  50% {
    border-color: var(--color-pink);
  }
}

.today-label {
  position: relative;
  z-index: 1;
  font-size: 0.85rem;
  font-weight: 700;
  color: var(--color-dark);
}

.day-number {
  position: relative;
  z-index: 1;
  font-size: 1.6rem;
  font-weight: 800;
  color: var(--color-dark);
  transition: color var(--transition);
  text-shadow: 0 1px 3px rgba(255, 255, 255, 0.6);
}

@media (prefers-reduced-motion: reduce) {
  .day-square {
    transition: none;
    animation: none;
  }
}
</style>
