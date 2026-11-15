import { describe, it, expect, beforeEach, vi } from 'vitest'

// useTheme keeps module-level state, so every test loads a fresh copy.
async function loadTheme() {
  vi.resetModules()
  const { useTheme } = await import('../../src/composables/useTheme')
  return useTheme()
}

describe('useTheme', () => {
  beforeEach(() => {
    delete document.documentElement.dataset.theme
  })

  it('follows the system by default (light in jsdom)', async () => {
    const { mode, resolved } = await loadTheme()

    expect(mode.value).toBe('system')
    expect(resolved.value).toBe('light')
    expect(document.documentElement.dataset.theme).toBe('light')
  })

  it('cycles system → light → dark → system and remembers the choice', async () => {
    const { mode, cycle } = await loadTheme()

    cycle()
    expect(mode.value).toBe('light')
    cycle()
    expect(mode.value).toBe('dark')
    expect(document.documentElement.dataset.theme).toBe('dark')
    expect(localStorage.getItem('theme')).toBe('dark')
    cycle()
    expect(mode.value).toBe('system')
  })

  it('starts with the stored choice', async () => {
    localStorage.setItem('theme', 'dark')

    const { mode, resolved } = await loadTheme()

    expect(mode.value).toBe('dark')
    expect(resolved.value).toBe('dark')
  })

  it('ignores invalid stored values', async () => {
    localStorage.setItem('theme', 'purple')

    const { mode } = await loadTheme()

    expect(mode.value).toBe('system')
  })
})
