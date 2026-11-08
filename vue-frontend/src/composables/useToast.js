import { ref } from 'vue'

// App-wide confirmation messages ("Photo posted"). Shared state, so any component can push one.
const toasts = ref([])
let nextId = 1

export function useToast() {
  function dismiss(id) {
    toasts.value = toasts.value.filter((t) => t.id !== id)
  }

  function show(message, { duration = 5000 } = {}) {
    const id = nextId++
    toasts.value.push({ id, message })
    setTimeout(() => dismiss(id), duration)
  }

  return { toasts, show, dismiss }
}
