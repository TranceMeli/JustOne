<script setup>
import { ref, onMounted } from 'vue'
import apiClient from '../api/axios'

const entries = ref([])
const newMessage = ref('')
const isLoading = ref(false)
const isSubmitting = ref(false)
const errorMessage = ref('')

async function fetchEntries() {
  isLoading.value = true
  errorMessage.value = ''

  try {
    const response = await apiClient.get('/guestbook')
    entries.value = response.data
  } catch {
    errorMessage.value = 'Could not load the guestbook.'
  } finally {
    isLoading.value = false
  }
}

async function submitEntry() {
  if (!newMessage.value.trim()) {
    return
  }

  isSubmitting.value = true
  errorMessage.value = ''

  try {
    const response = await apiClient.post('/guestbook', { message: newMessage.value.trim() })
    entries.value.unshift(response.data)
    newMessage.value = ''
  } catch {
    errorMessage.value = 'Could not save the entry.'
  } finally {
    isSubmitting.value = false
  }
}

function formatDate(dateString) {
  return new Date(dateString).toLocaleString('en-US', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

onMounted(fetchEntries)
</script>

<template>
  <div class="guestbook">
    <h3>Guestbook</h3>

    <form class="entry-form" @submit.prevent="submitEntry">
      <textarea
        v-model="newMessage"
        placeholder="Write something in the guestbook..."
        rows="3"
      />
      <button type="submit" :disabled="isSubmitting">
        {{ isSubmitting ? 'Saving...' : 'Post entry' }}
      </button>
    </form>

    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
    <p v-else-if="isLoading" class="status-text">Loading entries...</p>
    <p v-else-if="entries.length === 0" class="status-text">No entries yet.</p>

    <ul v-else class="entry-list">
      <li v-for="entry in entries" :key="entry.id" class="entry">
        <p class="entry-message">{{ entry.message }}</p>
        <span class="entry-date">{{ formatDate(entry.createdAt) }}</span>
      </li>
    </ul>
  </div>
</template>

<style scoped>
.guestbook {
  max-width: 600px;
  margin: 40px auto;
  color: var(--color-dark);
}

.guestbook h3 {
  text-align: center;
  margin-bottom: 12px;
}

.guestbook h3::after {
  content: '';
  display: block;
  width: 48px;
  height: 4px;
  margin: 6px auto 0;
  background: var(--color-pink);
  border-radius: 2px;
}

.entry-form {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

textarea {
  padding: 10px;
  border: 2px solid var(--color-muted);
  border-radius: var(--radius);
  background: var(--color-surface);
  color: var(--color-dark);
  font-family: inherit;
  resize: vertical;
}

.entry-form button {
  align-self: flex-end;
  padding: 8px 20px;
  background: var(--color-cyan);
  color: var(--color-dark);
  border: 2px solid var(--color-dark);
  border-radius: var(--radius);
  cursor: pointer;
  font-weight: 700;
}

.entry-form button:not(:disabled):hover {
  background: var(--color-pink);
  color: var(--color-light);
}

.entry-form button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.status-text {
  text-align: center;
  color: var(--color-muted);
}

.error {
  color: var(--color-pink);
  text-align: center;
  font-weight: 600;
}

.entry-list {
  list-style: none;
  padding: 0;
  margin-top: 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.entry {
  background: var(--color-surface);
  border: 1px solid var(--color-dark);
  border-left: 5px solid var(--color-cyan);
  border-radius: var(--radius);
  padding: 10px 14px;
  transition: border-color var(--transition), transform var(--transition);
}

.entry:hover {
  border-left-color: var(--color-pink);
  transform: translateX(3px);
}

.entry-message {
  margin: 0 0 4px;
  white-space: pre-wrap;
}

.entry-date {
  font-size: 0.8rem;
  color: var(--color-muted);
}
</style>
