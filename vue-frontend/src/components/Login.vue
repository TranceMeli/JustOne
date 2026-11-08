<script setup>
import { ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import AppIcon from './AppIcon.vue'

const emit = defineEmits(['switch-to-register'])

const authStore = useAuthStore()

// Seeded by the backend's DbSeeder - only for trying the app out.
const DEMO = { email: 'admin@justone.com', password: 'Admin123!' }

const email = ref('')
const password = ref('')
const errorMessage = ref('')
const isSubmitting = ref(false)

async function login(credentials) {
  errorMessage.value = ''
  isSubmitting.value = true
  try {
    await authStore.login(credentials.email, credentials.password)
  } catch (err) {
    errorMessage.value =
      err.response?.status === 401
        ? 'Email or password is wrong. Check both and try again.'
        : 'The server is not responding. Check your connection and try again.'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="auth">
    <section class="card" aria-labelledby="login-title">
      <div class="card-body">
        <h1 id="login-title">Welcome back</h1>
        <p class="sub">Log in to see your month.</p>

        <form @submit.prevent="login({ email, password })">
          <div class="field">
            <label for="login-email">Email</label>
            <input id="login-email" v-model="email" class="input" type="email" autocomplete="email" placeholder="you@example.com" required />
          </div>

          <div class="field">
            <label for="login-password">Password</label>
            <input id="login-password" v-model="password" class="input" type="password" autocomplete="current-password" required />
          </div>

          <div v-if="errorMessage" class="alert" role="alert">
            <AppIcon name="alert" />
            <span>{{ errorMessage }}</span>
          </div>

          <button type="submit" class="btn btn-primary btn-block" :disabled="isSubmitting">
            {{ isSubmitting ? 'Logging in…' : 'Log in' }}
          </button>

          <div class="divider">or</div>

          <button type="button" class="btn btn-secondary btn-block" :disabled="isSubmitting" @click="login(DEMO)">
            Try the demo account
          </button>

          <p class="switch">
            New here?
            <a href="#" class="text-link" @click.prevent="emit('switch-to-register')">Create an account</a>
          </p>
        </form>
      </div>
    </section>
  </div>
</template>

<style scoped>
.auth {
  display: grid;
  place-items: center;
  padding: var(--space-6) var(--space-4);
}

.card {
  width: min(400px, 100%);
}

h1 {
  margin: 0 0 4px;
  font-size: var(--text-xl);
  letter-spacing: -0.01em;
}

.sub {
  margin: 0 0 var(--space-5);
  color: var(--text-secondary);
}

form {
  display: grid;
  gap: var(--space-4);
}

.divider {
  display: flex;
  align-items: center;
  gap: 10px;
  color: var(--text-secondary);
  font-size: var(--text-xs);
}

.divider::before,
.divider::after {
  content: '';
  flex: 1;
  height: 1px;
  background: var(--border);
}

.switch {
  margin: 0;
  text-align: center;
  font-size: var(--text-sm);
}
</style>
