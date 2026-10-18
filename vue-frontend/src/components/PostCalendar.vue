<script setup>
defineProps({
  appName: { type: String, default: 'JustOne' },
  username: { type: String, required: true },
  monthLabel: { type: String, required: true },
  days: { type: Array, required: true }
})

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
      <span class="app-name">{{ appName }}</span>
      <span>Logged in as {{ username }}</span>
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
  font-family: system-ui, sans-serif;
  background: #eaeaea;
}

.calendar-header {
  background: #08d9d6;
  color: #252a34;
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 24px;
  font-weight: 700;
  font-size: 1.1rem;
}

.calendar-title {
  text-align: center;
  font-size: 1.8rem;
  font-weight: 800;
  color: #252a34;
  letter-spacing: 1px;
  margin: 28px 0;
}

.calendar-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(90px, 1fr));
  gap: 14px;
  padding: 0 24px 28px;
}

.day-square {
  position: relative;
  aspect-ratio: 1 / 1;
  border-radius: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

/* Separate layer for the SVG graphic. The SVG itself has no color (only brightness/shape) -
   the actual color comes from background-color and is clipped via a CSS mask. */
.square-bg {
  position: absolute;
  inset: 0;
  z-index: 0;
  background-color: #08d9d6;
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

.day-square.missed .square-bg {
  background-color: #ff2e63;
}

.day-square.future .square-bg,
.day-square.today .square-bg {
  display: none;
}

.day-square.future {
  border: 1px dashed #cfcfcf;
}

.day-square.today {
  background: #eaeaea;
  border: 2px dashed #252a34;
}

.today-label {
  position: relative;
  z-index: 1;
  font-size: 0.85rem;
  font-weight: 700;
  color: #252a34;
}

.day-number {
  position: relative;
  z-index: 1;
  font-size: 1.6rem;
  font-weight: 800;
  color: #252a34;
  text-shadow: 0 1px 3px rgba(255, 255, 255, 0.6);
}
</style>