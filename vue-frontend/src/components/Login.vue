<script setup>
import { ref } from 'vue'
import { useAuthStore } from '../stores/auth'

const emit = defineEmits(['switch-to-register'])

const authStore = useAuthStore()

const email = ref('admin@codewithmukesh.com')
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
  font-family: system-ui, sans-serif;
}

form {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

input {
  padding: 8px;
  border: 1px solid #ccc;
  border-radius: 4px;
}

button {
  padding: 8px;
  background: #4fc08d;
  color: #fff;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.error {
  color: #e63946;
}

.switch-link {
  text-align: center;
  margin-top: 12px;
}
</style>