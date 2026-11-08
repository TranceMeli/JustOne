<script setup>
import { useToast } from '../composables/useToast'
import AppIcon from './AppIcon.vue'

const { toasts, dismiss } = useToast()
</script>

<template>
  <div class="toast-host" role="status" aria-live="polite">
    <TransitionGroup name="toast">
      <div v-for="toast in toasts" :key="toast.id" class="toast">
        <AppIcon name="check" />
        <span>{{ toast.message }}</span>
        <button type="button" class="btn btn-ghost btn-icon" aria-label="Dismiss" @click="dismiss(toast.id)">
          <AppIcon name="close" />
        </button>
      </div>
    </TransitionGroup>
  </div>
</template>

<style scoped>
.toast-host {
  position: fixed;
  right: var(--space-4);
  bottom: var(--space-4);
  z-index: 2000;
  display: grid;
  gap: var(--space-2);
  width: min(380px, calc(100vw - 2 * var(--space-4)));
}

.toast {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-2) var(--space-2) var(--space-2) var(--space-4);
  border-radius: var(--radius-md);
  background: var(--gray-900);
  color: #e6e8ec;
  box-shadow: var(--shadow);
  font-size: var(--text-sm);
}

.toast > svg {
  width: 18px;
  height: 18px;
  flex: none;
  color: var(--indigo-300);
}

.toast span {
  flex: 1;
}

.toast .btn-ghost:hover {
  background: rgba(255, 255, 255, 0.08);
}

.toast-enter-active,
.toast-leave-active {
  transition: opacity 200ms ease-out, transform 200ms ease-out;
}

.toast-enter-from,
.toast-leave-to {
  opacity: 0;
  transform: translateY(8px);
}
</style>
