<script setup>
import { useAuthStore } from '../stores/auth'

const authStore = useAuthStore()
</script>

<template>
  <header class="app-header">
    <div class="header-inner">
      <a href="/" class="brand" aria-label="JustOne home">
        <span class="logo-mark" aria-hidden="true">1</span>
        <span class="logo-text">Just<span class="accent">One</span></span>
      </a>

      <span class="tagline">One photo per day.</span>

      <div v-if="authStore.isLoggedIn" class="user-area">
        <span class="avatar" aria-hidden="true">{{ authStore.email?.charAt(0).toUpperCase() }}</span>
        <span class="user-email" :title="authStore.email">{{ authStore.email }}</span>
        <button type="button" class="logout-button" @click="authStore.logout()">Log out</button>
      </div>
    </div>
  </header>
</template>

<style scoped>
.app-header {
  position: sticky;
  top: 0;
  z-index: 100;
  background: var(--color-dark);
  color: var(--color-light);
  border-bottom: 4px solid var(--color-cyan);
  box-shadow: 0 4px 0 var(--color-pink);
}

.header-inner {
  max-width: 960px;
  margin: 0 auto;
  padding: 12px 24px;
  display: flex;
  align-items: center;
  gap: 16px;
}

.brand {
  display: flex;
  align-items: center;
  gap: 10px;
  color: var(--color-light);
  text-decoration: none;
}

.brand:hover {
  color: var(--color-light);
  text-decoration: none;
}

.logo-mark {
  width: 34px;
  height: 34px;
  display: grid;
  place-items: center;
  background: var(--color-cyan);
  color: var(--color-dark);
  border: 2px solid var(--color-light);
  border-radius: var(--radius);
  box-shadow: 3px 3px 0 var(--color-pink);
  font-weight: 900;
  font-size: 1.2rem;
  transition: transform var(--transition);
}

.brand:hover .logo-mark {
  transform: rotate(-8deg);
}

.logo-text {
  font-size: 1.5rem;
  font-weight: 900;
  letter-spacing: 1px;
}

.accent {
  color: var(--color-cyan);
}

.tagline {
  padding-left: 16px;
  border-left: 2px solid var(--color-pink);
  font-size: 0.9rem;
  opacity: 0.8;
}

.user-area {
  margin-left: auto;
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.avatar {
  flex-shrink: 0;
  width: 32px;
  height: 32px;
  display: grid;
  place-items: center;
  border-radius: 50%;
  background: var(--color-pink);
  color: var(--color-light);
  font-weight: 800;
}

.user-email {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-weight: 600;
}

.logout-button {
  flex-shrink: 0;
  padding: 6px 14px;
  background: transparent;
  color: var(--color-pink);
  border: 2px solid var(--color-pink);
  border-radius: var(--radius);
  font-weight: 700;
  cursor: pointer;
}

.logout-button:hover {
  background: var(--color-pink);
  color: var(--color-light);
}

@media (max-width: 720px) {
  .tagline {
    display: none;
  }
}

@media (max-width: 480px) {
  .header-inner {
    padding: 10px 16px;
    gap: 10px;
  }

  .logo-text {
    font-size: 1.2rem;
  }

  .user-email {
    display: none;
  }
}
</style>
