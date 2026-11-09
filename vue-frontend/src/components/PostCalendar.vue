<script setup>
import { computed } from 'vue'
import AppIcon from './AppIcon.vue'

const props = defineProps({
  year: { type: Number, required: true },
  month: { type: Number, required: true },
  days: { type: Array, required: true },
  leadingBlanks: { type: Number, required: true },
  postedCount: { type: Number, required: true },
  elapsedCount: { type: Number, required: true },
  canGoPrev: { type: Boolean, required: true },
  canGoNext: { type: Boolean, required: true },
  isCurrentMonth: { type: Boolean, required: true },
  isLoading: { type: Boolean, default: false },
  errorMessage: { type: String, default: '' }
})

const emit = defineEmits(['prev', 'next', 'current', 'retry', 'select-day', 'add-today'])

const weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']

const monthLabel = computed(() =>
  new Date(props.year, props.month - 1, 1).toLocaleDateString('en-GB', { month: 'long', year: 'numeric' })
)
const monthName = computed(() => new Date(props.year, props.month - 1, 1).toLocaleDateString('en-GB', { month: 'long' }))
const progress = computed(() => (props.elapsedCount ? Math.round((props.postedCount / props.elapsedCount) * 100) : 0))

function dayLabel(d) {
  const date = `${d.day} ${monthName.value}`
  if (d.status === 'posted') {
    const what = d.kind === 'text' ? 'text' : 'photo'
    return d.isToday ? `Your ${what} for today, ${date}` : `${what === 'text' ? 'Text' : 'Photo'} from ${date}`
  }
  if (d.status === 'today') return `Today, ${date} – add a post`
  if (d.status === 'missed') return `${date} – missed`
  return date
}

function onDayClick(d) {
  if (d.status === 'posted') emit('select-day', d)
  else if (d.status === 'today') emit('add-today')
}
</script>

<template>
  <section class="card calendar" aria-labelledby="calendar-title">
    <div class="card-head calendar-head">
      <div class="month-nav">
        <button type="button" class="btn btn-ghost btn-icon" :disabled="!canGoPrev" aria-label="Previous month" @click="emit('prev')">
          <AppIcon name="prev" />
        </button>
        <h2 id="calendar-title" aria-live="polite">{{ monthLabel }}</h2>
        <button type="button" class="btn btn-ghost btn-icon" :disabled="!canGoNext" aria-label="Next month" @click="emit('next')">
          <AppIcon name="next" />
        </button>
        <button type="button" class="btn btn-secondary today-button" :disabled="isCurrentMonth" @click="emit('current')">
          Today
        </button>
      </div>

      <div v-if="!isLoading && !errorMessage" class="progress">
        <span><strong>{{ postedCount }}</strong> of {{ elapsedCount }} days posted</span>
        <span class="bar" role="img" :aria-label="`${postedCount} of ${elapsedCount} days posted`">
          <span :style="{ width: `${progress}%` }" />
        </span>
      </div>
    </div>

    <div class="card-body">
      <div class="alert" v-if="errorMessage" role="alert">
        <AppIcon name="alert" />
        <div>
          <strong>Calendar could not be loaded.</strong>
          {{ errorMessage }}
          <div class="retry">
            <button type="button" class="btn btn-secondary" @click="emit('retry')">
              <AppIcon name="refresh" />
              Try again
            </button>
          </div>
        </div>
      </div>

      <template v-else>
        <div class="weekdays" aria-hidden="true">
          <span v-for="w in weekdays" :key="w">{{ w }}</span>
        </div>

        <div v-if="isLoading" class="grid" aria-busy="true" aria-label="Loading calendar">
          <span v-for="n in 35" :key="n" class="skeleton day-skeleton" />
        </div>

        <ol v-else class="grid" :aria-label="monthLabel">
          <li v-for="n in leadingBlanks" :key="`blank-${n}`" class="blank" aria-hidden="true" />
          <li v-for="d in days" :key="d.day">
            <button
              v-if="d.status === 'posted' || d.status === 'today'"
              type="button"
              class="day"
              :class="[d.status, d.kind]"
              :aria-label="dayLabel(d)"
              @click="onDayClick(d)"
            >
              <img v-if="d.kind === 'image'" :src="d.imageUrl" alt="" loading="lazy" />
              <span v-else-if="d.kind === 'text'" class="excerpt" aria-hidden="true">
                <AppIcon name="quote" />
                <span>{{ d.text }}</span>
              </span>
              <span class="num">{{ d.day }}</span>
              <span v-if="d.status === 'today'" class="add">
                <AppIcon name="plus" />
                <span>Add post</span>
              </span>
            </button>
            <div v-else class="day" :class="d.status" :aria-label="dayLabel(d)" role="img">
              <span class="num">{{ d.day }}</span>
              <span v-if="d.status === 'missed'" class="status">
                <AppIcon name="close" />
                <span>missed</span>
              </span>
            </div>
          </li>
        </ol>

        <ul class="legend">
          <li><i class="l-posted" />Photo / GIF</li>
          <li><i class="l-text" />Text</li>
          <li><i class="l-missed" />Missed</li>
          <li><i class="l-today" />Today</li>
          <li><i class="l-future" />Upcoming</li>
        </ul>
      </template>
    </div>
  </section>
</template>

<style scoped>
.calendar-head {
  flex-wrap: wrap;
  gap: var(--space-3);
}

.month-nav {
  display: flex;
  align-items: center;
  gap: 4px;
}

.month-nav h2 {
  min-width: 9.5em;
  text-align: center;
}

.today-button {
  min-height: 32px;
  margin-left: var(--space-2);
}

.progress {
  margin-left: auto;
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: var(--text-sm);
  color: var(--text-secondary);
}

.progress strong {
  color: var(--text);
}

.bar {
  width: 96px;
  height: 8px;
  border-radius: 999px;
  background: var(--surface-sunken);
  border: 1px solid var(--border);
  overflow: hidden;
}

.bar span {
  display: block;
  height: 100%;
  background: var(--accent);
  transition: width 300ms ease-out;
}

.retry {
  margin-top: var(--space-3);
}

.retry .btn {
  color: var(--text);
}

.weekdays,
.grid {
  display: grid;
  grid-template-columns: repeat(7, minmax(0, 1fr));
  gap: var(--space-2);
}

.weekdays {
  margin-bottom: var(--space-2);
  text-align: center;
  font-size: var(--text-xs);
  font-weight: 600;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  color: var(--text-secondary);
}

.grid {
  list-style: none;
  margin: 0;
  padding: 0;
}

.day-skeleton {
  aspect-ratio: 1;
}

.day {
  position: relative;
  width: 100%;
  aspect-ratio: 1;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  align-items: flex-start;
  padding: 6px 8px;
  overflow: hidden;
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  background: var(--surface-sunken);
  color: var(--text-secondary);
  font-size: var(--text-sm);
  font-weight: 600;
  text-align: left;
}

button.day {
  cursor: pointer;
  transition: transform var(--motion), box-shadow var(--motion);
}

button.day:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow);
}

.num {
  position: relative;
  z-index: 1;
}

.day.posted {
  padding: 0;
  border-color: transparent;
}

.day.posted img {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  object-fit: cover;
}

/* Text posts: the first words on a soft indigo card */
.day.posted.text {
  background: var(--accent-soft);
  border-color: transparent;
}

.excerpt {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
  gap: 2px;
  padding: 28px 8px 8px;
  color: var(--text);
  font-size: var(--text-xs);
  font-weight: 400;
  line-height: 1.35;
}

.excerpt svg {
  position: absolute;
  top: 6px;
  right: 6px;
  width: 16px;
  height: 16px;
  color: var(--accent-text);
}

.excerpt span {
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.day.posted .num {
  margin: 6px 8px;
  padding: 0 6px;
  border-radius: 999px;
  background: rgba(26, 30, 39, 0.72);
  color: #fbfbfc;
  font-size: var(--text-xs);
  line-height: 20px;
}

.day.missed {
  border-color: transparent;
  background: var(--missed-bg);
  color: var(--missed);
}

.status {
  position: absolute;
  right: 7px;
  bottom: 6px;
  display: flex;
  align-items: center;
  gap: 3px;
  font-size: var(--text-xs);
}

.status svg,
.add svg {
  width: 14px;
  height: 14px;
}

.day.today {
  border: 2px dashed var(--focus);
  background: var(--surface);
  color: var(--accent-text);
}

.add {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: var(--text-xs);
}

.day.future {
  border-style: dashed;
  background: transparent;
  font-weight: 400;
}

:root[data-theme='dark'] .day.future {
  border-color: var(--gray-700);
}

.legend {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2) var(--space-4);
  margin: var(--space-4) 0 0;
  padding: 0;
  list-style: none;
  font-size: var(--text-sm);
  color: var(--text-secondary);
}

.legend li {
  display: flex;
  align-items: center;
  gap: 6px;
}

.legend i {
  width: 16px;
  height: 16px;
  border: 1px solid var(--border);
  border-radius: 4px;
}

.l-posted {
  background: linear-gradient(135deg, var(--indigo-300), var(--pink-300));
  border-color: transparent !important;
}

.l-text {
  background: var(--accent-soft);
  border-color: var(--accent-text) !important;
}

.l-missed {
  background: var(--missed-bg);
  border-color: var(--missed) !important;
}

.l-today {
  border: 2px dashed var(--focus) !important;
}

.l-future {
  border-style: dashed !important;
}

@media (max-width: 600px) {
  /* Too small for words - the quote icon alone marks a text day. */
  .excerpt span {
    display: none;
  }

  .excerpt svg {
    top: auto;
    bottom: 4px;
    right: 4px;
    width: 14px;
    height: 14px;
  }

  .card-head,
  .card-body {
    padding-left: var(--space-4);
    padding-right: var(--space-4);
  }

  .month-nav {
    width: 100%;
    justify-content: space-between;
  }

  .month-nav h2 {
    min-width: 0;
  }

  .today-button {
    display: none;
  }

  .progress {
    margin-left: 0;
    width: 100%;
  }

  .bar {
    flex: 1;
  }

  .weekdays,
  .grid {
    gap: 4px;
  }

  .day {
    padding: 4px 5px;
    font-size: var(--text-xs);
    border-radius: 8px;
  }

  .status span,
  .add span {
    display: none;
  }

  .status {
    right: 4px;
    bottom: 3px;
  }

  .day.posted .num {
    margin: 3px;
    padding: 0 5px;
    line-height: 18px;
  }
}
</style>
