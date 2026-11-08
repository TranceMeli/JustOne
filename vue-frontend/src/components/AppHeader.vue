<script setup>
import { computed } from 'vue'
import { useAuthStore } from '../stores/auth'
import { useTheme } from '../composables/useTheme'
import AppIcon from './AppIcon.vue'

const authStore = useAuthStore()
const { mode, cycle } = useTheme()

const themeInfo = computed(() => ({
  system: { icon: 'system', label: 'Theme: system' },
  light: { icon: 'sun', label: 'Theme: light' },
  dark: { icon: 'moon', label: 'Theme: dark' }
})[mode.value])
</script>

<template>
  <header class="app-header">
    <div class="inner">
      <a href="/" class="brand" aria-label="JustOne – home">
        <span class="brand-mark" aria-hidden="true">1</span>
        <span>Just<span class="one">One</span></span>
      </a>

      <div class="actions">
        <button
          type="button"
          class="btn btn-ghost btn-icon"
          :aria-label="`${themeInfo.label} (click to change)`"
          :title="themeInfo.label"
          @click="cycle"
        >
          <AppIcon :name="themeInfo.icon" />
        </button>

        <template v-if="authStore.isLoggedIn">
          <span class="user-chip">
            <span class="avatar" aria-hidden="true">{{ authStore.displayName.charAt(0).toUpperCase() }}</span>
            <span class="email">{{ authStore.email }}</span>
          </span>
          <button type="button" class="btn btn-ghost" @click="authStore.logout()">
            <AppIcon name="logout" />
            <span class="label">Log out</span>
          </button>
        </template>
      </div>
    </div>
  </header>
</template>

<style scoped>
.app-header {
  position: sticky;
  top: 0;
  z-index: 100;
  background: var(--surface-inverse);
  color: var(--text-inverse);
  border-bottom: 3px solid var(--brand-on-dark);
}

.inner {
  max-width: 1120px;
  margin: 0 auto;
  padding: 10px 24px;
  display: flex;
  align-items: center;
  gap: var(--space-4);
}

.brand {
  display: flex;
  align-items: center;
  gap: 10px;
  color: inherit;
  font-size: var(--text-lg);
  font-weight: 700;
  letter-spacing: -0.01em;
  text-decoration: none;
}

.brand-mark {
  width: 32px;
  height: 32px;
  display: grid;
  place-items: center;
  border-radius: 8px;
  background: var(--accent);
  color: var(--text-on-accent);
}

.one {
  color: var(--brand-on-dark);
}

.actions {
  margin-left: auto;
  display: flex;
  align-items: center;
  gap: var(--space-2);
  min-width: 0;
}

.actions .btn-ghost:not(:disabled):hover {
  background: rgba(255, 255, 255, 0.08);
}

.user-chip {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  min-width: 0;
  padding: 4px 12px 4px 4px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.06);
  font-size: var(--text-sm);
}

.user-chip .avatar {
  width: 28px;
  height: 28px;
}

.email {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

@media (max-width: 600px) {
  .inner {
    padding: 8px 16px;
  }

  .email,
  .label {
    display: none;
  }

  .user-chip {
    padding: 4px;
  }
}
</style>
