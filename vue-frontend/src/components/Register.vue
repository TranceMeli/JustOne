<script setup>
import { ref } from 'vue'
import { useAuthStore } from '../stores/auth'

const emit = defineEmits(['switch-to-login'])

const authStore = useAuthStore()

const firstName = ref('')
const lastName = ref('')
const email = ref('')
const password = ref('')
const errorMessage = ref('')
const isSubmitting = ref(false)

async function handleRegister() {
  errorMessage.value = ''
  isSubmitting.value = true

  try {
    await authStore.register(firstName.value, lastName.value, email.value, password.value)
  } catch (err) {
    if (err.response?.status === 400) {
      // Identity returns a list of reasons for weak passwords, duplicate emails, etc.
      const data = err.response.data
      errorMessage.value = Array.isArray(data) ? data.join(' ') : String(data)
    } else {
      errorMessage.value = 'Registration failed. Please try again.'
    }
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="register-box">
    <h2>Register</h2>

    <form @submit.prevent="handleRegister">
      <input v-model="firstName" type="text" placeholder="First name" required />
      <input v-model="lastName" type="text" placeholder="Last name" required />
      <input v-model="email" type="email" placeholder="Email" required />
      <input v-model="password" type="password" placeholder="Password" required />
      <button type="submit" :disabled="isSubmitting">
        {{ isSubmitting ? 'Registering...' : 'Register' }}
      </button>
    </form>

    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

    <p class="switch-link">
      Already have an account?
      <a href="#" @click.prevent="emit('switch-to-login')">Back to login</a>
    </p>
  </div>
</template>

<style scoped>
.register-box {
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
