<script setup>
import { ref, computed, onMounted } from 'vue'
import apiClient from '../api/axios'
import { useToast } from '../composables/useToast'
import AppIcon from './AppIcon.vue'

// Must match the [StringLength] on CreateGuestbookEntryRequest in the backend.
const MAX_LENGTH = 2000

const { show: showToast } = useToast()

const entries = ref([])
const newMessage = ref('')
const isLoading = ref(false)
const isSubmitting = ref(false)
const errorMessage = ref('')

const canPost = computed(() => newMessage.value.trim().length > 0 && !isSubmitting.value)

async function fetchEntries() {
  isLoading.value = true
  errorMessage.value = ''

  try {
    const response = await apiClient.get('/guestbook')
    entries.value = response.data
  } catch {
    errorMessage.value = 'The guestbook could not be loaded. Reload the page to try again.'
  } finally {
    isLoading.value = false
  }
}

async function submitEntry() {
  if (!canPost.value) return

  isSubmitting.value = true
  errorMessage.value = ''

  try {
    const response = await apiClient.post('/guestbook', { message: newMessage.value.trim() })
    entries.value.unshift(response.data)
    newMessage.value = ''
    showToast('Entry added to your guestbook.')
  } catch {
    errorMessage.value = 'Your entry could not be saved. Your text is still here – please try again.'
  } finally {
    isSubmitting.value = false
  }
}

function formatDate(dateString) {
  return new Date(dateString).toLocaleString('en-GB', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

onMounted(fetchEntries)
</script>

<template>
  <section class="card" aria-labelledby="guestbook-title">
    <div class="card-head">
      <h2 id="guestbook-title">Guestbook</h2>
      <span class="meta private">Only visible to you</span>
    </div>

    <div class="card-body">
      <form class="field" @submit.prevent="submitEntry">
        <label for="guestbook-message">New entry</label>
        <textarea
          id="guestbook-message"
          v-model="newMessage"
          class="input"
          :maxlength="MAX_LENGTH"
          placeholder="Write something…"
          rows="3"
        />
        <div class="form-row">
          <span class="field-help">{{ newMessage.length }} / {{ MAX_LENGTH }}</span>
          <button type="submit" class="btn btn-secondary" :disabled="!canPost">
            {{ isSubmitting ? 'Saving…' : 'Post entry' }}
          </button>
        </div>
      </form>

      <div v-if="errorMessage" class="alert" role="alert">
        <AppIcon name="alert" />
        <span>{{ errorMessage }}</span>
      </div>

      <div v-if="isLoading" class="entries" aria-label="Loading entries">
        <span v-for="n in 2" :key="n" class="skeleton entry-skeleton" />
      </div>

      <p v-else-if="entries.length === 0 && !errorMessage" class="empty">
        No entries yet – write down a thought, a plan or just how your day was.
      </p>

      <ul v-else class="entries">
        <li v-for="entry in entries" :key="entry.id" class="entry">
          <p>{{ entry.message }}</p>
          <time class="meta" :datetime="entry.createdAt">{{ formatDate(entry.createdAt) }}</time>
        </li>
      </ul>
    </div>
  </section>
</template>

<style scoped>
.private {
  margin-left: auto;
}

.card-body {
  display: grid;
  gap: var(--space-4);
}

.form-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-2);
}

.entries {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  gap: var(--space-2);
}

.entry {
  padding: var(--space-3) var(--space-4);
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  background: var(--surface-sunken);
}

.entry p {
  margin: 0;
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}

.entry-skeleton {
  height: 64px;
}

.empty {
  margin: 0;
  color: var(--text-secondary);
  font-size: var(--text-sm);
}
</style>
