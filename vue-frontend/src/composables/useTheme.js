import { ref, computed } from 'vue'

// 'system' follows the OS setting, 'light'/'dark' are explicit choices.
// The choice is a per-browser convenience, so localStorage is fine (and optional).
const STORAGE_KEY = 'theme'
const MODES = ['system', 'light', 'dark']

const media = window.matchMedia('(prefers-color-scheme: dark)')

function readStored() {
  try {
    const value = localStorage.getItem(STORAGE_KEY)
    return MODES.includes(value) ? value : 'system'
  } catch {
    return 'system'
  }
}

const mode = ref(readStored())
const systemDark = ref(media.matches)

const resolved = computed(() => (mode.value === 'system' ? (systemDark.value ? 'dark' : 'light') : mode.value))

function apply() {
  document.documentElement.dataset.theme = resolved.value
}

media.addEventListener('change', (event) => {
  systemDark.value = event.matches
  apply()
})

apply()

export function useTheme() {
  function cycle() {
    mode.value = MODES[(MODES.indexOf(mode.value) + 1) % MODES.length]
    try {
      localStorage.setItem(STORAGE_KEY, mode.value)
    } catch {
      // Storage blocked (private mode) - the choice just isn't remembered.
    }
    apply()
  }

  return { mode, resolved, cycle }
}
