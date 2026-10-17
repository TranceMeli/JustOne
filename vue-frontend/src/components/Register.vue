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

button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.error {
  color: #e63946;
}

.switch-link {
  text-align: center;
  margin-top: 12px;
}
</style>