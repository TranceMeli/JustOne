<script setup>
import { ref, computed, watch, nextTick, onBeforeUnmount } from 'vue'
import apiClient from '../api/axios'
import { useToast } from '../composables/useToast'
import AppIcon from './AppIcon.vue'
import ConfirmDialog from './ConfirmDialog.vue'

const props = defineProps({
  // Today's post ({ postId, kind: 'image', imageUrl } or { postId, kind: 'text', text }) or null
  post: { type: Object, default: null },
  // false until the current month has been loaded once
  known: { type: Boolean, default: false }
})

const emit = defineEmits(['posted', 'deleted'])

// Must match the limits in the backend's PostsController.
const MAX_FILE_SIZE = 10 * 1024 * 1024
const MAX_TEXT_LENGTH = 500
const ACCEPTED_TYPES = ['image/jpeg', 'image/png', 'image/gif', 'image/webp']

const { show: showToast } = useToast()

const fileInput = ref(null)
const textArea = ref(null)
const mode = ref('image') // 'image' | 'text'
const editing = ref(false) // true while changing an existing post
const selectedFile = ref(null)
const previewUrl = ref(null)
const draftText = ref('')
const isDragging = ref(false)
const isSubmitting = ref(false)
const errorMessage = ref('')
const confirmOpen = ref(false)

const hasPost = computed(() => !!props.post)
const showEditor = computed(() => !hasPost.value || editing.value)
const textLeft = computed(() => MAX_TEXT_LENGTH - draftText.value.length)
const canPostText = computed(() => draftText.value.trim().length > 0 && textLeft.value >= 0)
const todayLabel = computed(() =>
  new Date().toLocaleDateString('en-GB', { weekday: 'long', day: 'numeric', month: 'long' })
)

function clearFile() {
  if (previewUrl.value) URL.revokeObjectURL(previewUrl.value)
  previewUrl.value = null
  selectedFile.value = null
  if (fileInput.value) fileInput.value.value = ''
}

function resetEditor() {
  clearFile()
  draftText.value = ''
  errorMessage.value = ''
  editing.value = false
}

function setMode(next) {
  mode.value = next
  errorMessage.value = ''
  if (next === 'text') nextTick(() => textArea.value?.focus())
}

function pickFile(file) {
  errorMessage.value = ''
  clearFile()
  if (!file) return

  if (!ACCEPTED_TYPES.includes(file.type)) {
    errorMessage.value = 'This file type is not supported. Please choose a JPG, PNG, GIF or WebP image.'
    return
  }
  if (file.size > MAX_FILE_SIZE) {
    errorMessage.value = 'The file is larger than 10 MB. Please choose a smaller one or compress it first.'
    return
  }

  mode.value = 'image'
  selectedFile.value = file
  previewUrl.value = URL.createObjectURL(file)
}

// Also used by the calendar's "Today" tile.
function openPicker() {
  if (hasPost.value) editing.value = true
  mode.value = 'image'
  nextTick(() => fileInput.value?.click())
}

function startEditing() {
  editing.value = true
  mode.value = props.post?.kind === 'text' ? 'text' : 'image'
  draftText.value = props.post?.kind === 'text' ? props.post.text : ''
  if (mode.value === 'text') nextTick(() => textArea.value?.focus())
}

function onDrop(event) {
  isDragging.value = false
  pickFile(event.dataTransfer.files[0])
}

async function submit() {
  const formData = new FormData()
  if (mode.value === 'image') {
    if (!selectedFile.value) return
    formData.append('file', selectedFile.value)
  } else {
    if (!canPostText.value) return
    formData.append('text', draftText.value.trim())
  }

  isSubmitting.value = true
  errorMessage.value = ''

  try {
    // Don't set Content-Type manually - axios/the browser add the multipart boundary themselves.
    const wasReplace = hasPost.value
    const response = wasReplace ? await apiClient.put('/posts', formData) : await apiClient.post('/posts', formData)
    emit('posted', response.data)
    resetEditor()
    showToast(wasReplace ? 'Today’s post was updated.' : 'Posted for today. See you tomorrow!')
  } catch (err) {
    const status = err.response?.status
    if (status === 409) {
      errorMessage.value = 'You have already posted today. Use “Change post” to replace it.'
    } else if (status === 404) {
      errorMessage.value = 'There is no post for today anymore. Reload the page and try again.'
    } else if (status === 400 && typeof err.response.data === 'string') {
      errorMessage.value = err.response.data
    } else {
      errorMessage.value = 'Posting failed. Check your connection and try again.'
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
    resetEditor()
    showToast('Today’s post was deleted. You can post something new.')
  } catch {
    confirmOpen.value = false
    errorMessage.value = 'The post could not be deleted. Please try again.'
  } finally {
    isSubmitting.value = false
  }
}

// A new day or a deleted post closes a half-finished edit.
watch(() => props.post?.postId, () => {
  editing.value = false
})

defineExpose({ openPicker })

// Object URLs don't free themselves.
onBeforeUnmount(clearFile)
</script>

<template>
  <section class="card today-card" aria-labelledby="today-title">
    <div class="card-body">
      <div class="top">
        <p class="eyebrow">{{ todayLabel }}</p>
        <span v-if="hasPost && !editing" class="badge"><AppIcon name="check" />Posted</span>
      </div>
      <h2 id="today-title">
        {{ hasPost && !editing ? 'Today’s post' : editing ? 'Change today’s post' : 'What’s your one thing today?' }}
      </h2>

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

      <!-- Posted today -->
      <template v-else-if="!showEditor">
        <img v-if="post.kind === 'image'" :src="post.imageUrl" alt="Your post for today" class="photo" />
        <blockquote v-else class="text-post">{{ post.text }}</blockquote>
        <div class="row">
          <button type="button" class="btn btn-secondary" :disabled="isSubmitting" @click="startEditing">
            <AppIcon name="upload" />
            Change post
          </button>
          <button type="button" class="btn btn-danger-ghost" :disabled="isSubmitting" @click="confirmOpen = true">
            <AppIcon name="trash" />
            Delete
          </button>
        </div>
        <p class="hint">See you tomorrow – the next post is possible from midnight (your time zone).</p>
      </template>

      <!-- Editor: photo/GIF or text -->
      <template v-else>
        <p v-if="!editing" class="lead">One post per day: a photo, a GIF or a few words.</p>

        <div class="segmented" role="radiogroup" aria-label="Type of post">
          <button
            type="button"
            role="radio"
            :aria-checked="mode === 'image'"
            :class="{ active: mode === 'image' }"
            @click="setMode('image')"
          >
            <AppIcon name="image" />Photo / GIF
          </button>
          <button
            type="button"
            role="radio"
            :aria-checked="mode === 'text'"
            :class="{ active: mode === 'text' }"
            @click="setMode('text')"
          >
            <AppIcon name="text" />Text
          </button>
        </div>

        <!-- Photo / GIF -->
        <template v-if="mode === 'image'">
          <img v-if="previewUrl" :src="previewUrl" alt="Preview of the selected file" class="photo" />
          <div
            v-else
            class="drop"
            :class="{ dragging: isDragging }"
            @dragover.prevent="isDragging = true"
            @dragleave="isDragging = false"
            @drop.prevent="onDrop"
          >
            <AppIcon name="upload" />
            <strong>Drag a photo or GIF here</strong>
            <span>or</span>
            <button type="button" class="btn btn-primary" @click="openPicker">Choose file</button>
            <span class="hint">JPG, PNG, GIF or WebP · max. 10 MB</span>
          </div>
        </template>

        <!-- Text -->
        <div v-else class="field">
          <label for="today-text">Your words for today</label>
          <textarea
            id="today-text"
            ref="textArea"
            v-model="draftText"
            class="input text-input"
            :maxlength="MAX_TEXT_LENGTH"
            rows="6"
            placeholder="A short poem, a thought, a quote…"
            aria-describedby="today-text-help"
          />
          <span id="today-text-help" class="field-help" :class="{ warn: textLeft < 50 }">
            {{ textLeft }} characters left · line breaks are kept
          </span>
        </div>

        <div v-if="previewUrl || mode === 'text' || editing" class="row">
          <button
            type="button"
            class="btn btn-primary"
            :disabled="isSubmitting || (mode === 'image' ? !selectedFile : !canPostText)"
            @click="submit"
          >
            <AppIcon :name="mode === 'image' ? 'upload' : 'check'" />
            {{ isSubmitting ? 'Posting…' : editing ? 'Save' : 'Post' }}
          </button>
          <button v-if="previewUrl" type="button" class="btn btn-ghost" :disabled="isSubmitting" @click="clearFile">
            Choose another
          </button>
          <button v-if="editing" type="button" class="btn btn-ghost" :disabled="isSubmitting" @click="resetEditor">
            Cancel
          </button>
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
      title="Delete today’s post?"
      message="Your post and its comments will be removed. You can post something new today afterwards."
      confirm-label="Delete post"
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

/* Two-way switch: photo/GIF or text */
.segmented {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 4px;
  padding: 4px;
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  background: var(--surface-sunken);
}

.segmented button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  min-height: 36px;
  border: 0;
  border-radius: var(--radius-sm);
  background: transparent;
  color: var(--text-secondary);
  font-size: var(--text-sm);
  font-weight: 600;
  cursor: pointer;
}

.segmented button svg {
  width: 16px;
  height: 16px;
}

.segmented button.active {
  background: var(--surface);
  color: var(--text);
  box-shadow: var(--shadow);
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

.text-post {
  margin: 0;
  padding: var(--space-5) var(--space-4);
  border-radius: var(--radius-md);
  background: var(--accent-soft);
  color: var(--text);
  font-size: 1.125rem;
  line-height: 1.6;
  text-align: center;
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}

.text-input {
  min-height: 150px;
  font-size: 1.0625rem;
  line-height: 1.6;
}

.field-help.warn {
  color: var(--danger);
  font-weight: 600;
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
