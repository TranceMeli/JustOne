<script setup>
import { ref, watch, onMounted, onBeforeUnmount } from 'vue'
import Comments from './Comments.vue'

const props = defineProps({
  imageUrl: { type: String, default: null },
  postId: { type: Number, default: null }
})

const emit = defineEmits(['close'])

const isZoomed = ref(false)

// 'portrait' | 'landscape' - decides whether image and comments sit side by side or stacked.
const orientation = ref('portrait')

function onImageLoad(event) {
  const { naturalWidth, naturalHeight } = event.target
  orientation.value = naturalWidth > naturalHeight ? 'landscape' : 'portrait'
}

// Don't stay stuck in zoom mode when opening a different post (or closing).
watch(() => props.imageUrl, () => {
  isZoomed.value = false
})

function onKeydown(event) {
  if (event.key !== 'Escape' || !props.imageUrl) return
  if (isZoomed.value) {
    isZoomed.value = false
  } else {
    emit('close')
  }
}

onMounted(() => window.addEventListener('keydown', onKeydown))
onBeforeUnmount(() => window.removeEventListener('keydown', onKeydown))
</script>

<template>
  <Teleport to="body">
    <div v-if="imageUrl" class="modal-backdrop" @click="emit('close')">
      <div class="modal-content" :class="orientation" role="dialog" aria-modal="true" @click.stop>
        <button class="close-button" aria-label="Close" @click="emit('close')">&times;</button>
        <div class="thumb-frame">
          <img
            :src="imageUrl"
            alt="Post preview - click for full size"
            class="thumb"
            @load="onImageLoad"
            @click="isZoomed = true"
          />
        </div>
        <Comments :post-id="postId" />
      </div>
    </div>

    <!-- Second layer: full-size image, on top of the box -->
    <div v-if="imageUrl && isZoomed" class="zoom-backdrop" @click="isZoomed = false">
      <img :src="imageUrl" alt="Post at full size" class="zoom-image" />
    </div>
  </Teleport>
</template>

<style scoped>
.modal-backdrop {
  position: fixed;
  inset: 0;
  padding: 16px;
  background: rgba(37, 42, 52, 0.85);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  position: relative;
  display: flex;
  gap: 20px;
  background: var(--color-dark);
  border: 2px solid var(--color-cyan);
  border-radius: 8px;
  box-shadow: 8px 8px 0 var(--color-pink);
  padding: 20px;
}

/* Portrait image: image left, comments right. */
.modal-content.portrait {
  flex-direction: row;
  width: min(92vw, 900px);
  height: min(85vh, 640px);
}

.modal-content.portrait .thumb-frame {
  flex: 0 1 auto;
  max-width: 55%;
  height: 100%;
}

.modal-content.portrait .comments {
  min-width: 260px;
}

/* Landscape image: image on top across the full width, comments below. */
.modal-content.landscape {
  flex-direction: column;
  width: min(92vw, 760px);
  height: min(90vh, 820px);
}

.modal-content.landscape .thumb-frame {
  flex: 0 1 auto;
  width: 100%;
  max-height: 55%;
}

.thumb-frame {
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 0;
  min-height: 0;
  background: var(--color-dark-deep);
  border-radius: var(--radius);
  overflow: hidden;
}

.thumb {
  display: block;
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
  border: 2px solid transparent;
  border-radius: var(--radius);
  cursor: zoom-in;
  transition: border-color var(--transition);
}

.modal-content.portrait .thumb {
  height: 100%;
}

/* A percentage max-height would not resolve here (the frame has no fixed height). */
.modal-content.landscape .thumb {
  width: 100%;
  max-height: min(48vh, 440px);
}

.thumb:hover {
  border-color: var(--color-cyan);
}

/* Portrait screens (phones, upright tablets): always stack, whatever the image format. */
@media (max-width: 700px), (orientation: portrait) and (max-width: 1024px) {
  .modal-content.portrait,
  .modal-content.landscape {
    flex-direction: column;
    width: min(94vw, 560px);
    height: min(90vh, 860px);
    gap: 14px;
    padding: 16px;
  }

  .modal-content.portrait .thumb-frame,
  .modal-content.landscape .thumb-frame {
    width: 100%;
    max-width: none;
    height: auto;
    max-height: 50%;
  }

  .modal-content.portrait .thumb,
  .modal-content.landscape .thumb {
    width: auto;
    height: auto;
    max-height: min(42vh, 420px);
  }

  .modal-content.portrait .comments {
    min-width: 0;
  }
}

.zoom-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.92);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1100;
  cursor: zoom-out;
}

.zoom-image {
  max-width: 95vw;
  max-height: 95vh;
  object-fit: contain;
  border: 3px solid var(--color-light);
  border-radius: var(--radius);
}

.close-button {
  position: absolute;
  top: -14px;
  right: -14px;
  z-index: 1;
  width: 36px;
  height: 36px;
  border-radius: 50%;
  border: 2px solid var(--color-dark);
  background: var(--color-pink);
  color: var(--color-light);
  font-size: 1.4rem;
  line-height: 1;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
}

.close-button:hover {
  background: var(--color-cyan);
  color: var(--color-dark);
  transform: rotate(90deg);
}
</style>
