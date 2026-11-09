<script setup>
import { ref, computed, watch, nextTick } from 'vue'
import apiClient from '../api/axios'
import AppIcon from './AppIcon.vue'

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
    errorMessage.value = 'Comments could not be loaded. Close the photo and open it again.'
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
    errorMessage.value = 'Your comment could not be sent. Your text is still here – please try again.'
  } finally {
    isSubmitting.value = false
  }
}

function formatDate(dateString) {
  return new Date(dateString).toLocaleString('en-GB', {
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit'
  })
}

// Reload whenever the displayed post changes (e.g. a different photo opened in the modal).
watch(() => props.postId, fetchComments, { immediate: true })
</script>

<template>
  <section class="comments" aria-labelledby="comments-title">
    <h3 id="comments-title">
      <AppIcon name="chat" />
      Comments
      <span class="count">{{ comments.length }}</span>
    </h3>

    <div v-if="isLoading" class="loading" aria-label="Loading comments">
      <span v-for="n in 2" :key="n" class="skeleton comment-skeleton" />
    </div>

    <div v-else-if="comments.length === 0 && !errorMessage" class="empty">
      <AppIcon name="chat" />
      <strong>No comments yet</strong>
      <span>Be the first to say something about this photo.</span>
    </div>

    <ul v-else ref="list" class="comment-list">
      <li v-for="comment in comments" :key="comment.id" class="comment">
        <span class="avatar" aria-hidden="true">{{ comment.authorName.charAt(0).toUpperCase() }}</span>
        <div>
          <div class="who">
            <strong>{{ comment.authorName }}</strong>
            <span v-if="comment.isOwn" class="you">You</span>
            <time class="meta" :datetime="comment.createdAt">{{ formatDate(comment.createdAt) }}</time>
          </div>
          <p class="message">{{ comment.message }}</p>
        </div>
      </li>
    </ul>

    <form class="composer" @submit.prevent="submitComment">
      <div v-if="errorMessage" class="alert" role="alert">
        <AppIcon name="alert" />
        <span>{{ errorMessage }}</span>
      </div>

      <div v-if="showEmojis" class="emoji-row" role="group" aria-label="Emojis">
        <button
          v-for="emoji in quickEmojis"
          :key="emoji"
          type="button"
          class="emoji-button"
          :aria-label="`Insert ${emoji}`"
          @click="insertEmoji(emoji)"
        >
          {{ emoji }}
        </button>
      </div>

      <div class="input-bar">
        <button
          type="button"
          class="btn btn-ghost btn-icon emoji-toggle"
          :aria-pressed="showEmojis"
          aria-label="Insert emoji"
          title="Insert emoji"
          @click="showEmojis = !showEmojis"
        >
          😊
        </button>

        <label for="comment-input" class="visually-hidden">Comment</label>
        <textarea
          id="comment-input"
          ref="textarea"
          v-model="newMessage"
          rows="1"
          :maxlength="MAX_LENGTH"
          placeholder="Write a comment…"
          @input="autoGrow"
          @keydown="onKeydown"
        />

        <button type="submit" class="btn btn-primary" :disabled="!canSend">
          {{ isSubmitting ? 'Sending…' : 'Send' }}
        </button>
      </div>

      <div class="meta hint">
        <span>Enter to send · Shift+Enter for a new line</span>
        <span v-if="remaining < 200" :class="{ warn: remaining < 50 }">{{ remaining }} characters left</span>
      </div>
    </form>
  </section>
</template>

<style scoped>
.comments {
  min-width: 0;
  min-height: 0;
  display: grid;
  grid-template-rows: auto minmax(0, 1fr) auto;
}

h3 {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  margin: 0;
  padding: var(--space-4) var(--space-5) var(--space-2);
  font-size: var(--text-md);
}

h3 svg {
  width: 18px;
  height: 18px;
  color: var(--accent-text);
}

.count {
  min-width: 22px;
  padding: 0 7px;
  border: 1px solid var(--border);
  border-radius: 999px;
  background: var(--surface-sunken);
  color: var(--text-secondary);
  font-size: var(--text-xs);
  font-weight: 600;
  text-align: center;
}

.loading {
  display: grid;
  gap: var(--space-3);
  align-content: start;
  padding: var(--space-2) var(--space-5);
}

.comment-skeleton {
  height: 52px;
}

.empty {
  display: grid;
  justify-items: center;
  align-content: center;
  gap: var(--space-2);
  padding: var(--space-5);
  text-align: center;
  color: var(--text-secondary);
}

.empty svg {
  width: 32px;
  height: 32px;
  color: var(--accent-text);
}

.empty strong {
  color: var(--text);
}

.comment-list {
  list-style: none;
  margin: 0;
  padding: var(--space-2) var(--space-5);
  display: grid;
  gap: var(--space-4);
  align-content: start;
  overflow-y: auto;
}

.comment {
  display: grid;
  grid-template-columns: 32px minmax(0, 1fr);
  gap: 10px;
}

.who {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 0 var(--space-2);
  font-size: var(--text-sm);
}

.who strong {
  font-weight: 600;
  overflow-wrap: anywhere;
}

.you {
  padding: 0 6px;
  border-radius: 999px;
  background: var(--accent-soft);
  color: var(--accent-text);
  font-size: var(--text-xs);
  font-weight: 600;
}

.message {
  margin: 2px 0 0;
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}

.composer {
  display: grid;
  gap: 6px;
  padding: var(--space-3) var(--space-5) var(--space-4);
  border-top: 1px solid var(--border);
}

.emoji-row {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  padding: 6px;
  border: 1px solid var(--border);
  border-radius: var(--radius-md);
  background: var(--surface-sunken);
}

.emoji-button {
  min-width: 40px;
  min-height: 40px;
  border: 0;
  border-radius: var(--radius-sm);
  background: transparent;
  font-size: 1.25rem;
  line-height: 1;
  cursor: pointer;
}

.emoji-button:hover {
  background: var(--accent-soft);
}

/* One bar: emoji toggle, growing text field, send button */
.input-bar {
  display: flex;
  align-items: flex-end;
  gap: 6px;
  padding: 6px;
  border: 1px solid var(--border-strong);
  border-radius: var(--radius-md);
  background: var(--surface);
  transition: border-color var(--motion), box-shadow var(--motion);
}

.input-bar:focus-within {
  border-color: var(--focus);
  box-shadow: 0 0 0 1px var(--focus);
}

.input-bar textarea {
  flex: 1;
  min-width: 0;
  max-height: 120px;
  padding: 8px 4px;
  border: 0;
  outline: 0;
  background: transparent;
  color: var(--text);
  font-size: var(--text-md);
  resize: none;
  overflow-y: auto;
}

.input-bar textarea::placeholder {
  color: var(--text-secondary);
}

.emoji-toggle {
  font-size: 1.2rem;
}

.emoji-toggle[aria-pressed='true'] {
  background: var(--accent-soft);
}

.hint {
  display: flex;
  justify-content: space-between;
  gap: var(--space-2);
}

.hint .warn {
  color: var(--danger);
  font-weight: 600;
}

@media (hover: none) {
  .hint span:first-child {
    visibility: hidden;
  }
}
</style>
