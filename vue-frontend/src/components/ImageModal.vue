<script setup>
import { ref, watch } from 'vue'
import Comments from './Comments.vue'

const props = defineProps({
  imageUrl: { type: String, default: null },
  postId: { type: Number, default: null }
})

const emit = defineEmits(['close'])

const isZoomed = ref(false)

// Don't stay stuck in zoom mode when opening a different post (or closing).
watch(() => props.imageUrl, () => {
  isZoomed.value = false
})
</script>

<template>
  <Teleport to="body">
    <div v-if="imageUrl" class="modal-backdrop" @click="emit('close')">
      <div class="modal-content" @click.stop>
        <button class="close-button" @click="emit('close')">&times;</button>
        <img
          :src="imageUrl"
          alt="Post preview - click for full size"
          class="thumb"
          @click="isZoomed = true"
        />
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
  background: rgba(37, 42, 52, 0.85);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  position: relative;
  /* Fixed size instead of following the content - the box stays the same size for every post. */
  width: clamp(320px, 75vw, 860px);
  height: clamp(320px, 70vh, 520px);
  display: flex;
  flex-direction: row;
  align-items: stretch;
  gap: 20px;
  background: #252a34;
  border-radius: 8px;
  padding: 20px;
  overflow: hidden;
}

.thumb {
  /* Fixed width, full height of the box - but the whole image stays visible (no cropping). */
  flex: 0 0 280px;
  width: 280px;
  height: 100%;
  object-fit: contain;
  background: #1a1e27;
  display: block;
  border-radius: 4px;
  cursor: zoom-in;
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
  border-radius: 4px;
}

/* Stack vertically instead of side by side on narrow screens. */
@media (max-width: 640px) {
  .modal-content {
    flex-direction: column;
    width: min(92vw, 420px);
    height: min(85vh, 640px);
    overflow-y: auto;
  }

  .thumb {
    flex: 0 0 200px;
    width: 100%;
    height: 200px;
  }
}

.close-button {
  position: absolute;
  top: -14px;
  right: -14px;
  width: 36px;
  height: 36px;
  border-radius: 50%;
  border: none;
  background: #ff2e63;
  color: #eaeaea;
  font-size: 1.4rem;
  line-height: 1;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
}
</style>