<script setup>
import { ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import AppIcon from './AppIcon.vue'

const emit = defineEmits(['switch-to-login'])

const authStore = useAuthStore()

const firstName = ref('')
const lastName = ref('')
const email = ref('')
const password = ref('')
const errors = ref([])
const passwordInvalid = ref(false)
const isSubmitting = ref(false)

async function handleRegister() {
  errors.value = []
  passwordInvalid.value = false
  isSubmitting.value = true

  try {
    await authStore.register(firstName.value, lastName.value, email.value, password.value)
  } catch (err) {
    if (err.response?.status === 400) {
      // Identity returns a list of reasons (weak password, email taken, ...).
      const data = err.response.data
      errors.value = Array.isArray(data) ? data : [typeof data === 'string' ? data : 'Please check your entries.']
      passwordInvalid.value = errors.value.some((e) => /password/i.test(e))
    } else {
      errors.value = ['The server is not responding. Check your connection and try again.']
    }
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="auth">
    <section class="card" aria-labelledby="register-title">
      <div class="card-body">
        <h1 id="register-title">Create account</h1>
        <p class="sub">One photo per day. That’s it.</p>

        <form @submit.prevent="handleRegister">
          <div class="name-row">
            <div class="field">
              <label for="reg-first">First name</label>
              <input id="reg-first" v-model="firstName" class="input" autocomplete="given-name" required />
            </div>
            <div class="field">
              <label for="reg-last">Last name</label>
              <input id="reg-last" v-model="lastName" class="input" autocomplete="family-name" required />
            </div>
          </div>

          <div class="field">
            <label for="reg-email">Email</label>
            <input id="reg-email" v-model="email" class="input" type="email" autocomplete="email" required />
          </div>

          <div class="field">
            <label for="reg-password">Password</label>
            <input
              id="reg-password"
              v-model="password"
              class="input"
              type="password"
              autocomplete="new-password"
              aria-describedby="reg-password-help"
              :aria-invalid="passwordInvalid"
              required
            />
            <span id="reg-password-help" class="field-help">
              At least 6 characters with an uppercase letter, a lowercase letter, a digit and a symbol.
            </span>
          </div>

          <div v-if="errors.length" class="alert" role="alert">
            <AppIcon name="alert" />
            <div>
              <strong>Your account could not be created.</strong>
              <ul>
                <li v-for="e in errors" :key="e">{{ e }}</li>
              </ul>
            </div>
          </div>

          <button type="submit" class="btn btn-primary btn-block" :disabled="isSubmitting">
            {{ isSubmitting ? 'Creating account…' : 'Create account' }}
          </button>

          <p class="switch">
            Already registered?
            <a href="#" class="text-link" @click.prevent="emit('switch-to-login')">Log in</a>
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
  width: min(440px, 100%);
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

.name-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-3);
}

.alert ul {
  margin: 4px 0 0;
  padding-left: 18px;
}

.switch {
  margin: 0;
  text-align: center;
  font-size: var(--text-sm);
}

@media (max-width: 400px) {
  .name-row {
    grid-template-columns: 1fr;
  }
}
</style>
