<script setup>
import { ref, computed, watch, nextTick } from 'vue'
import apiClient from '../api/axios'

const props = defineProps({
  postId: { type: Number, default: null }
})

// Must match the [StringLength] on CreateCommentRequest in the backend.
const MAX_LENGTH = 1000

const comments = ref([])
const newMessage = ref('')
const isLoading = ref(false)
const isSubmitting = ref(false)
const errorMessage = ref('')
const showEmojis = ref(false)

const textarea = ref(null)
const list = ref(null)

const quickEmojis = ['😀', '😂', '😍', '❤️', '👍', '🔥', '🎉', '😮', '😢', '🙏']

const canSend = computed(() => newMessage.value.trim().length > 0 && !isSubmitting.value)
const remaining = computed(() => MAX_LENGTH - newMessage.value.length)

// Inserts at the cursor instead of always appending at the end.
function insertEmoji(emoji) {
  const el = textarea.value
  const start = el?.selectionStart ?? newMessage.value.length
  const end = el?.selectionEnd ?? newMessage.value.length

  newMessage.value = newMessage.value.slice(0, start) + emoji + newMessage.value.slice(end)

  nextTick(() => {
    el?.focus()
    el?.setSelectionRange(start + emoji.length, start + emoji.length)
    autoGrow()
  })
}

// Grows with the text up to the max-height set in CSS, then scrolls.
function autoGrow() {
  const el = textarea.value
  if (!el) return
  el.style.height = 'auto'
  el.style.height = `${el.scrollHeight}px`
}

// Enter sends, Shift+Enter makes a new line (like in common chat apps).
function onKeydown(event) {
  if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
    event.preventDefault()
    submitComment()
  }
}

function scrollToBottom() {
  nextTick(() => {
    if (list.value) {
      list.value.scrollTop = list.value.scrollHeight
    }
  })
}

async function fetchComments() {
  newMessage.value = ''
  showEmojis.value = false

  if (!props.postId) {
    comments.value = []
    return
  }

  isLoading.value = true
  errorMessage.value = ''

  try {
    const response = await apiClient.get(`/posts/${props.postId}/comments`)
    comments.value = response.data
    scrollToBottom()
  } catch {
    errorMessage.value = 'Could not load comments.'
  } finally {
    isLoading.value = false
  }
}

async function submitComment() {
  if (!canSend.value || !props.postId) {
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
    showEmojis.value = false
    nextTick(autoGrow)
    scrollToBottom()
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
    <h4>
      Comments
      <span v-if="comments.length" class="count">{{ comments.length }}</span>
    </h4>

    <p v-if="isLoading" class="status-text">Loading comments...</p>
    <p v-else-if="comments.length === 0" class="status-text">No comments yet. Be the first!</p>

    <ul v-else ref="list" class="comment-list">
      <li v-for="comment in comments" :key="comment.id" class="comment">
        <div class="comment-meta">
          <span class="comment-author">{{ comment.authorEmail }}</span>
          <span class="comment-date">{{ formatDate(comment.createdAt) }}</span>
        </div>
        <p class="comment-message">{{ comment.message }}</p>
      </li>
    </ul>

    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

    <form class="comment-form" @submit.prevent="submitComment">
      <div v-if="showEmojis" class="emoji-row">
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

      <div class="input-bar">
        <button
          type="button"
          class="emoji-toggle"
          :class="{ active: showEmojis }"
          :aria-pressed="showEmojis"
          aria-label="Show emojis"
          @click="showEmojis = !showEmojis"
        >
          😊
        </button>

        <textarea
          ref="textarea"
          v-model="newMessage"
          rows="1"
          :maxlength="MAX_LENGTH"
          placeholder="Write a comment..."
          aria-label="Comment"
          @input="autoGrow"
          @keydown="onKeydown"
        />

        <button type="submit" class="send-button" :disabled="!canSend" aria-label="Send comment">
          {{ isSubmitting ? '...' : 'Send' }}
        </button>
      </div>

      <div class="form-hint">
        <span>Enter to send · Shift+Enter for a new line</span>
        <span v-if="remaining < 200" :class="{ warn: remaining < 50 }">{{ remaining }}</span>
      </div>
    </form>
  </div>
</template>

<style scoped>
.comments {
  flex: 1;
  min-width: 0;
  min-height: 0;
  display: flex;
  flex-direction: column;
  font-family: system-ui, 'Apple Color Emoji', 'Segoe UI Emoji', 'Noto Color Emoji', sans-serif;
  color: var(--color-light);
}

.comments h4 {
  display: flex;
  align-items: center;
  gap: 8px;
  margin: 0 0 10px;
  color: var(--color-cyan);
}

.count {
  min-width: 22px;
  padding: 1px 7px;
  border-radius: 999px;
  background: var(--color-pink);
  color: var(--color-light);
  font-size: 0.75rem;
  text-align: center;
}

.status-text {
  flex: 1;
  margin: 8px 0;
  opacity: 0.8;
}

.error {
  margin: 6px 0;
  color: var(--color-pink);
  font-weight: 600;
}

/* The list takes all free space and scrolls, the form stays pinned at the bottom. */
.comment-list {
  list-style: none;
  padding: 0 4px 0 0;
  margin: 0 0 10px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  scrollbar-color: var(--color-cyan) transparent;
}

.comment {
  background: var(--color-light-soft);
  border-left: 3px solid var(--color-pink);
  border-radius: var(--radius);
  padding: 8px 12px;
  transition: background-color var(--transition), border-color var(--transition);
}

.comment:hover {
  background: var(--color-cyan-soft);
  border-left-color: var(--color-cyan);
}

.comment-meta {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  gap: 8px;
}

.comment-author {
  font-weight: 700;
  font-size: 0.85rem;
  color: var(--color-cyan);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.comment-date {
  flex-shrink: 0;
  font-size: 0.75rem;
  opacity: 0.7;
}

.comment-message {
  margin: 4px 0 0;
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}

.comment-form {
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding-top: 10px;
  border-top: 1px solid var(--color-light-soft);
}

.emoji-row {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  padding: 6px;
  background: var(--color-dark-deep);
  border: 1px solid var(--color-cyan);
  border-radius: var(--radius);
}

.emoji-button {
  background: transparent;
  border: 1px solid transparent;
  border-radius: var(--radius);
  padding: 4px 6px;
  font-size: 1.2rem;
  line-height: 1;
  cursor: pointer;
}

.emoji-button:hover {
  background: var(--color-cyan-soft);
  border-color: var(--color-cyan);
  transform: scale(1.15);
}

/* One rounded bar: emoji toggle, growing text field, send button. */
.input-bar {
  display: flex;
  align-items: flex-end;
  gap: 6px;
  padding: 4px;
  background: var(--color-light);
  border: 2px solid transparent;
  border-radius: 8px;
  transition: border-color var(--transition), box-shadow var(--transition);
}

.input-bar:focus-within {
  border-color: var(--color-cyan);
  box-shadow: 0 0 0 3px var(--color-cyan-soft);
}

.input-bar textarea {
  flex: 1;
  min-width: 0;
  max-height: 96px;
  padding: 7px 4px;
  border: none;
  background: transparent;
  color: var(--color-dark);
  font: inherit;
  line-height: 1.35;
  resize: none;
  overflow-y: auto;
}

.input-bar textarea:focus {
  box-shadow: none;
}

.emoji-toggle {
  flex-shrink: 0;
  width: 34px;
  height: 34px;
  border: none;
  border-radius: 6px;
  background: transparent;
  font-size: 1.2rem;
  cursor: pointer;
}

.emoji-toggle:hover,
.emoji-toggle.active {
  background: var(--color-cyan-soft);
}

.send-button {
  flex-shrink: 0;
  height: 34px;
  padding: 0 16px;
  background: var(--color-cyan);
  color: var(--color-dark);
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-weight: 700;
}

.send-button:not(:disabled):hover {
  background: var(--color-pink);
  color: var(--color-light);
}

.send-button:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.form-hint {
  display: flex;
  justify-content: space-between;
  font-size: 0.7rem;
  opacity: 0.6;
}

.form-hint .warn {
  color: var(--color-pink);
  font-weight: 700;
  opacity: 1;
}

@media (hover: none) {
  .form-hint span:first-child {
    visibility: hidden;
  }
}
</style>
