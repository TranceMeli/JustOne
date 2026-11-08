<script setup>
import { ref, watch, nextTick, onBeforeUnmount } from 'vue'
import AppIcon from './AppIcon.vue'

// Asks before an action that can't be undone (e.g. deleting a photo).
const props = defineProps({
  open: { type: Boolean, default: false },
  title: { type: String, required: true },
  message: { type: String, default: '' },
  confirmLabel: { type: String, default: 'Confirm' },
  busy: { type: Boolean, default: false }
})

const emit = defineEmits(['confirm', 'cancel'])

const cancelButton = ref(null)
let returnFocusTo = null

function onKeydown(event) {
  if (event.key === 'Escape' && !props.busy) emit('cancel')
}

watch(
  () => props.open,
  async (open) => {
    if (open) {
      returnFocusTo = document.activeElement
      window.addEventListener('keydown', onKeydown)
      await nextTick()
      // The safe choice gets the focus.
      cancelButton.value?.focus()
    } else {
      window.removeEventListener('keydown', onKeydown)
      returnFocusTo?.focus?.()
    }
  }
)

onBeforeUnmount(() => window.removeEventListener('keydown', onKeydown))
</script>

<template>
  <Teleport to="body">
    <div v-if="open" class="backdrop" @click.self="!busy && emit('cancel')">
      <div class="dialog" role="alertdialog" aria-modal="true" aria-labelledby="confirm-title" aria-describedby="confirm-message">
        <h2 id="confirm-title">{{ title }}</h2>
        <p id="confirm-message">{{ message }}</p>
        <div class="actions">
          <button ref="cancelButton" type="button" class="btn btn-secondary" :disabled="busy" @click="emit('cancel')">
            Cancel
          </button>
          <button type="button" class="btn btn-danger" :disabled="busy" @click="emit('confirm')">
            <AppIcon name="trash" />
            {{ busy ? 'Deleting…' : confirmLabel }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.backdrop {
  position: fixed;
  inset: 0;
  z-index: 1500;
  display: grid;
  place-items: center;
  padding: var(--space-4);
  background: var(--overlay);
}

.dialog {
  width: min(420px, 100%);
  display: grid;
  gap: var(--space-3);
  padding: var(--space-5);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  background: var(--surface);
  color: var(--text);
  box-shadow: var(--shadow);
}

h2 {
  margin: 0;
  font-size: var(--text-lg);
}

p {
  margin: 0;
  color: var(--text-secondary);
}

.actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: var(--space-2);
  margin-top: var(--space-2);
}
</style>
