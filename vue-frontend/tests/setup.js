import { afterEach, vi } from 'vitest'

// jsdom has no matchMedia - useTheme.js reads it when the module loads.
if (!window.matchMedia) {
  window.matchMedia = (query) => ({
    matches: false,
    media: query,
    addEventListener: () => {},
    removeEventListener: () => {}
  })
}

afterEach(() => {
  localStorage.clear()
  vi.restoreAllMocks()
})
