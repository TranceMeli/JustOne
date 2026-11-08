<script setup>
import { ref, computed, onBeforeUnmount } from 'vue'
import apiClient from '../api/axios'
import { BACKEND_ORIGIN } from '../config'
import { useToast } from '../composables/useToast'
import AppIcon from './AppIcon.vue'
import ConfirmDialog from './ConfirmDialog.vue'

const props = defineProps({
  // { imageUrl, postId } of today's post, or null
  post: { type: Object, default: null },
  // false until the current month has been loaded once
  known: { type: Boolean, default: false }
})

const emit = defineEmits(['posted', 'deleted'])

// Must match the limits in the backend's PostsController.
const MAX_FILE_SIZE = 10 * 1024 * 1024
const ACCEPTED_TYPES = ['image/jpeg', 'image/png', 'image/gif', 'image/webp']

const { show: showToast } = useToast()

const fileInput = ref(null)
const selectedFile = ref(null)
const previewUrl = ref(null)
const isDragging = ref(false)
const isSubmitting = ref(false)
const errorMessage = ref('')
const confirmOpen = ref(false)

const hasPost = computed(() => !!props.post)
const todayLabel = computed(() =>
  new Date().toLocaleDateString('en-GB', { weekday: 'long', day: 'numeric', month: 'long' })
)

function clearPreview() {
  if (previewUrl.value) URL.revokeObjectURL(previewUrl.value)
  previewUrl.value = null
  selectedFile.value = null
  if (fileInput.value) fileInput.value.value = ''
}

function pickFile(file) {
  errorMessage.value = ''
  clearPreview()
  if (!file) return

  if (!ACCEPTED_TYPES.includes(file.type)) {
    errorMessage.value = 'This file type is not supported. Please choose a JPG, PNG, GIF or WebP image.'
    return
  }
  if (file.size > MAX_FILE_SIZE) {
    errorMessage.value = 'The photo is larger than 10 MB. Please choose a smaller one or compress it first.'
    return
  }

  selectedFile.value = file
  previewUrl.value = URL.createObjectURL(file)
}

function openPicker() {
  fileInput.value?.click()
}

function onDrop(event) {
  isDragging.value = false
  pickFile(event.dataTransfer.files[0])
}

async function upload() {
  if (!selectedFile.value) return

  isSubmitting.value = true
  errorMessage.value = ''

  const formData = new FormData()
  formData.append('file', selectedFile.value)

  try {
    // Don't set Content-Type manually - axios/the browser add the multipart boundary themselves.
    const response = hasPost.value ? await apiClient.put('/posts', formData) : await apiClient.post('/posts', formData)
    const wasReplace = hasPost.value
    emit('posted', `${BACKEND_ORIGIN}${response.data.imageUrl}`, response.data.id)
    clearPreview()
    showToast(wasReplace ? 'Today’s photo was replaced.' : 'Photo posted for today. See you tomorrow!')
  } catch (err) {
    const status = err.response?.status
    if (status === 409) {
      errorMessage.value = 'You have already posted today. Use “Replace photo” to change it.'
    } else if (status === 404) {
      errorMessage.value = 'There is no photo for today anymore. Reload the page and try again.'
    } else if (status === 400 && typeof err.response.data === 'string') {
      errorMessage.value = err.response.data
    } else {
      errorMessage.value = 'The upload failed. Check your connection and try again.'
    }
  } finally {
    isSubmitting.value = false
  }
}

async function confirmDelete() {
  isSubmitting.value = true
  errorMessage.value = ''

  try {
    await apiClient.delete('/posts')
    confirmOpen.value = false
    emit('deleted')
    showToast('Today’s photo was deleted. You can post a new one.')
  } catch {
    confirmOpen.value = false
    errorMessage.value = 'The photo could not be deleted. Please try again.'
  } finally {
    isSubmitting.value = false
  }
}

// Lets the calendar's "Today" tile open the file picker.
defineExpose({ openPicker })

// Object URLs don't free themselves.
onBeforeUnmount(clearPreview)
</script>

<template>
  <section class="card today-card" aria-labelledby="today-title">
    <div class="card-body">
      <div class="top">
        <p class="eyebrow">{{ todayLabel }}</p>
        <span v-if="hasPost && !previewUrl" class="badge"><AppIcon name="check" />Posted</span>
      </div>
      <h2 id="today-title">{{ hasPost ? 'Today’s photo' : 'What’s your photo today?' }}</h2>

      <input
        ref="fileInput"
        class="visually-hidden"
        type="file"
        :accept="ACCEPTED_TYPES.join(',')"
        tabindex="-1"
        aria-hidden="true"
        @change="pickFile($event.target.files[0])"
      />

      <!-- Loading: today's status isn't known yet -->
      <div v-if="!known" class="skeleton photo-skeleton" aria-label="Loading today’s status" />

      <!-- A photo was chosen: preview + confirm -->
      <template v-else-if="previewUrl">
        <img :src="previewUrl" alt="Preview of the selected photo" class="photo" />
        <div class="row">
          <button type="button" class="btn btn-primary" :disabled="isSubmitting" @click="upload">
            <AppIcon name="upload" />
            {{ isSubmitting ? 'Uploading…' : hasPost ? 'Replace photo' : 'Post photo' }}
          </button>
          <button type="button" class="btn btn-ghost" :disabled="isSubmitting" @click="clearPreview">Cancel</button>
        </div>
      </template>

      <!-- Posted today -->
      <template v-else-if="hasPost">
        <img :src="post.imageUrl" alt="Your photo for today" class="photo" />
        <div class="row">
          <button type="button" class="btn btn-secondary" :disabled="isSubmitting" @click="openPicker">
            <AppIcon name="upload" />
            Replace photo
          </button>
          <button type="button" class="btn btn-danger-ghost" :disabled="isSubmitting" @click="confirmOpen = true">
            <AppIcon name="trash" />
            Delete
          </button>
        </div>
        <p class="hint">See you tomorrow – the next photo can be posted from midnight (your time zone).</p>
      </template>

      <!-- Not posted yet: drop zone -->
      <template v-else>
        <p class="lead">You can post exactly one photo per day.</p>
        <div
          class="drop"
          :class="{ dragging: isDragging }"
          @dragover.prevent="isDragging = true"
          @dragleave="isDragging = false"
          @drop.prevent="onDrop"
        >
          <AppIcon name="upload" />
          <strong>Drag a photo here</strong>
          <span>or</span>
          <button type="button" class="btn btn-primary" @click="openPicker">Choose photo</button>
          <span class="hint">JPG, PNG, GIF or WebP · max. 10 MB</span>
        </div>
      </template>

      <div v-if="errorMessage" class="alert" role="alert">
        <AppIcon name="alert" />
        <span>{{ errorMessage }}</span>
      </div>
    </div>

    <ConfirmDialog
      :open="confirmOpen"
      :busy="isSubmitting"
      title="Delete today’s photo?"
      message="The photo and its comments will be removed. You can post a new photo today afterwards."
      confirm-label="Delete photo"
      @confirm="confirmDelete"
      @cancel="confirmOpen = false"
    />
  </section>
</template>

<style scoped>
.card-body {
  display: grid;
  gap: var(--space-4);
}

.top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-2);
}

.eyebrow {
  margin: 0;
  font-size: var(--text-xs);
  font-weight: 600;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--accent-text);
}

h2 {
  margin: calc(-1 * var(--space-2)) 0 0;
  font-size: var(--text-lg);
}

.lead {
  margin: calc(-1 * var(--space-2)) 0 0;
  color: var(--text-secondary);
  font-size: var(--text-sm);
}

.badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 2px 10px;
  border-radius: 999px;
  background: var(--success-bg);
  color: var(--success);
  font-size: var(--text-sm);
  font-weight: 600;
}

.badge svg {
  width: 16px;
  height: 16px;
}

.photo,
.photo-skeleton {
  width: 100%;
  aspect-ratio: 4 / 3;
  border-radius: var(--radius-md);
}

.photo {
  object-fit: cover;
  background: var(--surface-sunken);
}

.drop {
  display: grid;
  place-items: center;
  gap: var(--space-2);
  padding: var(--space-5) var(--space-4);
  border: 2px dashed var(--border-strong);
  border-radius: var(--radius-md);
  background: var(--surface-sunken);
  color: var(--text-secondary);
  font-size: var(--text-sm);
  text-align: center;
  transition: border-color var(--motion), background-color var(--motion);
}

.drop.dragging {
  border-color: var(--focus);
  background: var(--accent-soft);
}

.drop > svg {
  width: 32px;
  height: 32px;
  color: var(--accent-text);
}

.drop strong {
  color: var(--text);
  font-size: var(--text-md);
}

.row {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
}

.hint {
  margin: 0;
  font-size: var(--text-xs);
  color: var(--text-secondary);
}
</style>
