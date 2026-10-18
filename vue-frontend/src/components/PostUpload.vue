<script setup>
import { ref, computed, onBeforeUnmount } from 'vue'
import apiClient from '../api/axios'
import { BACKEND_ORIGIN } from '../config'

const props = defineProps({
  existingImageUrl: { type: String, default: null }
})

const emit = defineEmits(['posted', 'deleted'])

// Must match the limits in the backend's PostsController.
const MAX_FILE_SIZE = 10 * 1024 * 1024
const ACCEPTED_TYPES = 'image/jpeg,image/png,image/gif,image/webp'

const fileInput = ref(null)
const selectedFile = ref(null)
const previewUrl = ref(null)
const isSubmitting = ref(false)
const errorMessage = ref('')

const isEditing = computed(() => !!props.existingImageUrl)

function clearPreview() {
  if (previewUrl.value) {
    URL.revokeObjectURL(previewUrl.value)
  }
  previewUrl.value = null
}

function onFileChange(event) {
  const file = event.target.files[0] ?? null
  selectedFile.value = file
  errorMessage.value = ''

  clearPreview()
  if (file && file.size > MAX_FILE_SIZE) {
    errorMessage.value = 'The photo is too large (max. 10 MB).'
    resetFileInput()
    return
  }
  if (file) {
    previewUrl.value = URL.createObjectURL(file)
  }
}

function resetFileInput() {
  selectedFile.value = null
  if (fileInput.value) {
    fileInput.value.value = ''
  }
}

async function handleSubmit() {
  if (!selectedFile.value) {
    errorMessage.value = 'Please choose a photo first.'
    return
  }

  isSubmitting.value = true
  errorMessage.value = ''

  const formData = new FormData()
  formData.append('file', selectedFile.value)

  try {
    // Don't set Content-Type manually - axios/the browser set the multipart boundary themselves
    const response = isEditing.value
      ? await apiClient.put('/posts', formData)
      : await apiClient.post('/posts', formData)

    emit('posted', `${BACKEND_ORIGIN}${response.data.imageUrl}`, response.data.id)
    resetFileInput()
    clearPreview()
  } catch (err) {
    if (err.response?.status === 409) {
      errorMessage.value = 'You have already posted today.'
    } else if (err.response?.status === 404) {
      errorMessage.value = 'No post found for today.'
    } else if (err.response?.status === 400 && typeof err.response.data === 'string') {
      errorMessage.value = err.response.data
    } else {
      errorMessage.value = 'Something went wrong. Please try again.'
    }
  } finally {
    isSubmitting.value = false
  }
}

async function handleDelete() {
  isSubmitting.value = true
  errorMessage.value = ''

  try {
    await apiClient.delete('/posts')
    emit('deleted')
  } catch {
    errorMessage.value = 'Delete failed. Please try again.'
  } finally {
    isSubmitting.value = false
  }
}

// Object URLs don't free themselves - clean up when leaving the page.
onBeforeUnmount(clearPreview)
</script>

<template>
  <div class="upload-box">
    <img
      v-if="previewUrl || existingImageUrl"
      :src="previewUrl ?? existingImageUrl"
      :alt="previewUrl ? 'Preview of the selected photo' : 'Your post for today'"
      class="preview"
    />

    <input ref="fileInput" type="file" :accept="ACCEPTED_TYPES" @change="onFileChange" />

    <div class="actions">
      <button :disabled="isSubmitting" @click="handleSubmit">
        {{ isSubmitting ? 'Working...' : isEditing ? 'Change photo' : 'Post today' }}
      </button>
      <button v-if="isEditing" class="delete-button" :disabled="isSubmitting" @click="handleDelete">
        Delete photo
      </button>
    </div>

    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
  </div>
</template>

<style scoped>
.upload-box {
  max-width: 400px;
  margin: 28px auto 0;
  padding: 20px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  align-items: center;
  border: 2px dashed var(--color-cyan);
  border-radius: var(--radius);
  background: var(--color-cyan-soft);
}

.preview {
  width: 120px;
  height: 120px;
  object-fit: cover;
  border: 3px solid var(--color-dark);
  border-radius: var(--radius);
  box-shadow: 4px 4px 0 var(--color-pink);
}

input[type='file'] {
  color: var(--color-dark);
}

input[type='file']::file-selector-button {
  margin-right: 10px;
  padding: 6px 12px;
  background: var(--color-light);
  color: var(--color-dark);
  border: 2px solid var(--color-dark);
  border-radius: var(--radius);
  font-weight: 600;
  cursor: pointer;
  transition: background-color var(--transition);
}

input[type='file']::file-selector-button:hover {
  background: var(--color-cyan);
}

.actions {
  display: flex;
  gap: 10px;
}

button {
  padding: 10px 20px;
  background: var(--color-pink);
  color: var(--color-light);
  border: 2px solid var(--color-pink);
  border-radius: var(--radius);
  cursor: pointer;
  font-weight: 700;
}

button:not(:disabled):hover {
  background: var(--color-dark);
  border-color: var(--color-dark);
}

.delete-button {
  background: transparent;
  color: var(--color-dark);
  border-color: var(--color-dark);
}

.delete-button:not(:disabled):hover {
  background: var(--color-pink);
  border-color: var(--color-pink);
  color: var(--color-light);
}

button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.error {
  color: var(--color-pink);
  font-weight: 600;
}
</style>
