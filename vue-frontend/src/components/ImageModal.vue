<script setup>
import { ref, computed, watch, nextTick, onBeforeUnmount } from 'vue'
import Comments from './Comments.vue'
import AppIcon from './AppIcon.vue'

const props = defineProps({
  // The opened calendar day ({ day, imageUrl, postId }) or null when closed
  entry: { type: Object, default: null },
  year: { type: Number, required: true },
  month: { type: Number, required: true },
  // All posted days of the month, for previous/next navigation
  postedDays: { type: Array, default: () => [] }
})

const emit = defineEmits(['close', 'navigate'])

const isZoomed = ref(false)
const closeButton = ref(null)
let returnFocusTo = null

// 'portrait' | 'landscape' - decides whether photo and comments sit side by side or stacked.
const orientation = ref('portrait')

const title = computed(() =>
  props.entry
    ? new Date(props.year, props.month - 1, props.entry.day).toLocaleDateString('en-GB', {
        weekday: 'long',
        day: 'numeric',
        month: 'long'
      })
    : ''
)

const index = computed(() => props.postedDays.findIndex((d) => d.day === props.entry?.day))
const prevEntry = computed(() => (index.value > 0 ? props.postedDays[index.value - 1] : null))
const nextEntry = computed(() =>
  index.value !== -1 && index.value < props.postedDays.length - 1 ? props.postedDays[index.value + 1] : null
)

function onImageLoad(event) {
  const { naturalWidth, naturalHeight } = event.target
  orientation.value = naturalWidth > naturalHeight ? 'landscape' : 'portrait'
}

function onKeydown(event) {
  // Don't steal arrow keys while the user is typing a comment.
  const typing = ['TEXTAREA', 'INPUT'].includes(document.activeElement?.tagName)

  if (event.key === 'Escape') {
    if (isZoomed.value) isZoomed.value = false
    else emit('close')
  } else if (!typing && event.key === 'ArrowLeft' && prevEntry.value) {
    emit('navigate', prevEntry.value)
  } else if (!typing && event.key === 'ArrowRight' && nextEntry.value) {
    emit('navigate', nextEntry.value)
  }
}

watch(
  () => !!props.entry,
  async (open) => {
    if (open) {
      returnFocusTo = document.activeElement
      window.addEventListener('keydown', onKeydown)
      document.body.style.overflow = 'hidden'
      await nextTick()
      closeButton.value?.focus()
    } else {
      window.removeEventListener('keydown', onKeydown)
      document.body.style.overflow = ''
      returnFocusTo?.focus?.()
    }
  }
)

// Don't stay stuck in zoom mode when switching to a different photo.
watch(
  () => props.entry?.postId,
  () => {
    isZoomed.value = false
    // Text posts sit next to the comments, like a portrait photo.
    if (props.entry?.kind === 'text') orientation.value = 'portrait'
  },
  { immediate: true }
)

onBeforeUnmount(() => {
  window.removeEventListener('keydown', onKeydown)
  document.body.style.overflow = ''
})
</script>

<template>
  <Teleport to="body">
    <div v-if="entry" class="backdrop" @click.self="emit('close')">
      <div class="modal" :class="orientation" role="dialog" aria-modal="true" aria-labelledby="modal-title">
        <header class="modal-head">
          <button
            type="button"
            class="btn btn-ghost btn-icon"
            :disabled="!prevEntry"
            aria-label="Previous post"
            @click="emit('navigate', prevEntry)"
          >
            <AppIcon name="prev" />
          </button>
          <h2 id="modal-title">{{ title }}</h2>
          <button
            type="button"
            class="btn btn-ghost btn-icon"
            :disabled="!nextEntry"
            aria-label="Next post"
            @click="emit('navigate', nextEntry)"
          >
            <AppIcon name="next" />
          </button>
          <button ref="closeButton" type="button" class="btn btn-secondary close" @click="emit('close')">
            <AppIcon name="close" />
            <span class="close-label">Close</span>
          </button>
        </header>

        <div v-if="entry.kind === 'image'" class="photo-area">
          <button type="button" class="photo-button" aria-label="Show photo at full size" @click="isZoomed = true">
            <img :src="entry.imageUrl" :alt="`Photo from ${title}`" @load="onImageLoad" />
          </button>
          <span class="zoom-hint" aria-hidden="true">Click to enlarge</span>
        </div>

        <div v-else class="text-area">
          <AppIcon name="quote" />
          <blockquote>{{ entry.text }}</blockquote>
        </div>

        <Comments :post-id="entry.postId" />
      </div>
    </div>

    <!-- Second layer: full-size photo -->
    <div v-if="entry?.kind === 'image' && isZoomed" class="zoom-backdrop" role="button" aria-label="Close full size view" @click="isZoomed = false">
      <img :src="entry.imageUrl" :alt="`Photo from ${title} at full size`" class="zoom-image" />
    </div>
  </Teleport>
</template>

<style scoped>
.backdrop {
  position: fixed;
  inset: 0;
  z-index: 1000;
  display: grid;
  place-items: center;
  padding: var(--space-4);
  background: var(--overlay);
}

.modal {
  display: grid;
  overflow: hidden;
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  background: var(--surface);
  color: var(--text);
  box-shadow: var(--shadow);
}

/* Portrait photo: photo left, comments right */
.modal.portrait {
  width: min(920px, 100%);
  height: min(620px, 100%);
  grid-template-columns: minmax(0, 1.15fr) minmax(300px, 1fr);
  grid-template-rows: auto minmax(0, 1fr);
}

/* Landscape photo: photo on top, comments below */
.modal.landscape {
  width: min(780px, 100%);
  height: min(860px, 100%);
  grid-template-columns: minmax(0, 1fr);
  grid-template-rows: auto auto minmax(0, 1fr);
}

.modal-head {
  grid-column: 1 / -1;
  display: flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-3) var(--space-3) var(--space-3) var(--space-4);
  border-bottom: 1px solid var(--border);
}

.modal-head h2 {
  margin: 0;
  font-size: var(--text-lg);
}

.close {
  margin-left: auto;
}

.photo-area {
  position: relative;
  min-height: 0;
  display: grid;
  place-items: center;
  background: var(--gray-950);
}

.modal.landscape .photo-area {
  max-height: min(48vh, 460px);
}

.photo-button {
  display: grid;
  place-items: center;
  width: 100%;
  height: 100%;
  padding: 0;
  border: 0;
  background: none;
  cursor: zoom-in;
}

.photo-button img {
  display: block;
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
}

.modal.landscape .photo-button img {
  max-height: min(48vh, 460px);
}

.text-area {
  position: relative;
  min-height: 0;
  display: grid;
  place-items: center;
  padding: var(--space-6) var(--space-5);
  overflow-y: auto;
  background: var(--accent-soft);
}

.text-area > svg {
  position: absolute;
  top: var(--space-4);
  left: var(--space-4);
  width: 32px;
  height: 32px;
  color: var(--accent-text);
  opacity: 0.6;
}

.text-area blockquote {
  margin: 0;
  max-width: 34ch;
  color: var(--text);
  font-size: var(--text-lg);
  line-height: 1.7;
  text-align: center;
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}

.zoom-hint {
  position: absolute;
  left: var(--space-3);
  bottom: var(--space-3);
  padding: 4px 10px;
  border-radius: 999px;
  background: rgba(26, 30, 39, 0.75);
  color: #e6e8ec;
  font-size: var(--text-xs);
  pointer-events: none;
}

.zoom-backdrop {
  position: fixed;
  inset: 0;
  z-index: 1100;
  display: grid;
  place-items: center;
  background: rgba(0, 0, 0, 0.92);
  cursor: zoom-out;
}

.zoom-image {
  max-width: 95vw;
  max-height: 95vh;
  object-fit: contain;
  border-radius: var(--radius-sm);
}

/* Phones and upright tablets: always full screen and stacked */
@media (max-width: 700px), (orientation: portrait) and (max-width: 1024px) {
  .backdrop {
    padding: 0;
  }

  .modal.portrait,
  .modal.landscape {
    width: 100%;
    height: 100%;
    border-radius: 0;
    grid-template-columns: minmax(0, 1fr);
    grid-template-rows: auto auto minmax(0, 1fr);
  }

  .photo-area,
  .modal.landscape .photo-area {
    max-height: 40vh;
  }

  .photo-button img,
  .modal.landscape .photo-button img {
    max-height: 40vh;
  }

  .close-label {
    display: none;
  }

  .close {
    width: 40px;
    padding: 0;
  }
}
</style>
