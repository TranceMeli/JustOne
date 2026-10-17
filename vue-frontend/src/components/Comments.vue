<script setup>
import { ref, watch } from 'vue'
import apiClient from '../api/axios'

const props = defineProps({
  postId: { type: Number, default: null }
})

const comments = ref([])
const newMessage = ref('')
const isLoading = ref(false)
const isSubmitting = ref(false)
const errorMessage = ref('')

const quickEmojis = ['😀', '😂', '❤️', '👍', '🔥', '🎉', '😮', '😢']

function insertEmoji(emoji) {
  newMessage.value += emoji
}

async function fetchComments() {
  if (!props.postId) {
    comments.value = []
    return
  }

  isLoading.value = true
  errorMessage.value = ''

  try {
    const response = await apiClient.get(`/posts/${props.postId}/comments`)
    comments.value = response.data
  } catch {
    errorMessage.value = 'Could not load comments.'
  } finally {
    isLoading.value = false
  }
}

async function submitComment() {
  if (!newMessage.value.trim() || !props.postId) {
    return
  }

  isSubmitting.value = true
  errorMessage.value = ''

  try {
    const response = await apiClient.post(`/posts/${props.postId}/comments`, {
      message: newMessage.value.trim()
    })
    comments.value.push(response.data)
    newMessage.value = ''
  } catch {
    errorMessage.value = 'Could not save the comment.'
  } finally {
    isSubmitting.value = false
  }
}

function formatDate(dateString) {
  return new Date(dateString).toLocaleString('en-US', {
    day: '2-digit',
    month: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

// Reload whenever the displayed post changes (e.g. a different photo opened in the modal).
watch(() => props.postId, fetchComments, { immediate: true })
</script>

<template>
  <div class="comments">
    <h4>Comments</h4>

    <div class="emoji-row">
      <button
        v-for="emoji in quickEmojis"
        :key="emoji"
        type="button"
        class="emoji-button"
        @click="insertEmoji(emoji)"
      >
        {{ emoji }}
      </button>
    </div>

    <form class="comment-form" @submit.prevent="submitComment">
      <input v-model="newMessage" type="text" placeholder="Write a comment..." />
      <button type="submit" :disabled="isSubmitting">
        {{ isSubmitting ? '...' : 'Send' }}
      </button>
    </form>

    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
    <p v-else-if="isLoading" class="status-text">Loading comments...</p>
    <p v-else-if="comments.length === 0" class="status-text">No comments yet.</p>

    <ul v-else class="comment-list">
      <li v-for="comment in comments" :key="comment.id" class="comment">
        <span class="comment-author">{{ comment.authorEmail }}</span>
        <p class="comment-message">{{ comment.message }}</p>
        <span class="comment-date">{{ formatDate(comment.createdAt) }}</span>
      </li>
    </ul>
  </div>
</template>

<style scoped>
.comments {
  flex: 1;
  min-width: 0;
  height: 100%;
  display: flex;
  flex-direction: column;
  font-family: system-ui, 'Apple Color Emoji', 'Segoe UI Emoji', 'Noto Color Emoji', sans-serif;
  color: #eaeaea;
}

.comments h4 {
  margin: 0 0 8px;
}

.emoji-row {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  margin-bottom: 8px;
}

.emoji-button {
  background: rgba(234, 234, 234, 0.1);
  border: none;
  border-radius: 4px;
  padding: 4px 6px;
  font-size: 1.1rem;
  line-height: 1;
  cursor: pointer;
}

.emoji-button:hover {
  background: rgba(234, 234, 234, 0.2);
}

.comment-form {
  display: flex;
  gap: 8px;
}

.comment-form input {
  flex: 1;
  padding: 8px;
  border: none;
  border-radius: 4px;
}

.comment-form button {
  padding: 8px 16px;
  background: #08d9d6;
  color: #252a34;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-weight: 700;
}

.comment-form button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.status-text {
  margin-top: 8px;
  opacity: 0.8;
}

.error {
  color: #ff2e63;
  font-weight: 600;
}

.comment-list {
  list-style: none;
  padding: 0;
  margin-top: 12px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  flex: 1;
  min-height: 0;
  overflow-y: auto;
}

.comment {
  background: rgba(234, 234, 234, 0.1);
  border-radius: 4px;
  padding: 8px 12px;
}

.comment-author {
  font-weight: 700;
  font-size: 0.85rem;
  color: #08d9d6;
}

.comment-message {
  margin: 2px 0;
}

.comment-date {
  font-size: 0.75rem;
  opacity: 0.7;
}
</style>