<script setup>
import { ref } from 'vue'
import { useAuthStore } from '../stores/auth'

const emit = defineEmits(['switch-to-register'])

const authStore = useAuthStore()

const email = ref('admin@justone.com')
const password = ref('Admin123!')
const errorMessage = ref('')

async function handleLogin() {
  errorMessage.value = ''
  try {
    await authStore.login(email.value, password.value)
  } catch (err) {
    errorMessage.value = 'Login failed. Please check your email and password.'
  }
}
</script>

<template>
  <div class="login-box">
    <h2>Login</h2>

    <form @submit.prevent="handleLogin">
      <input v-model="email" type="email" placeholder="Email" />
      <input v-model="password" type="password" placeholder="Password" />
      <button type="submit">Log in</button>
    </form>

    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
    <p v-if="authStore.isLoggedIn">Logged in as {{ authStore.email }}</p>

    <p class="switch-link">
      Don't have an account?
      <a href="#" @click.prevent="emit('switch-to-register')">Register now</a>
    </p>
  </div>
</template>

<style scoped>
.login-box {
  max-width: 320px;
  margin: 40px auto;
  padding: 24px;
  background: var(--color-light);
  border: 2px solid var(--color-dark);
  border-top: 6px solid var(--color-cyan);
  border-radius: var(--radius);
  box-shadow: 6px 6px 0 var(--color-pink);
}

h2 {
  margin-top: 0;
  color: var(--color-dark);
}

form {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

input {
  padding: 8px;
  border: 2px solid var(--color-muted);
  border-radius: var(--radius);
  background: var(--color-surface);
  color: var(--color-dark);
}

button {
  padding: 8px;
  background: var(--color-cyan);
  color: var(--color-dark);
  border: 2px solid var(--color-dark);
  border-radius: var(--radius);
  cursor: pointer;
  font-weight: 700;
}

button:not(:disabled):hover {
  background: var(--color-pink);
  color: var(--color-light);
}

button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.error {
  color: var(--color-pink);
  font-weight: 600;
}

.switch-link {
  text-align: center;
  margin-top: 12px;
}
</style>
