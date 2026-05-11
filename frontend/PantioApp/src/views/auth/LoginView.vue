<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../../stores/auth'
import PButton from '../../components/ui/PButton.vue'

const auth = useAuthStore()
const router = useRouter()

onMounted(async () => {
  await auth.initialize()
  if (auth.isAuthenticated) {
    router.replace('/')
  }
})
</script>

<template>
  <div class="login-page">
    <div class="login-card">
      <div class="login-mark" aria-hidden="true">
        <svg width="56" height="56" viewBox="0 0 56 56" fill="none" xmlns="http://www.w3.org/2000/svg">
          <circle cx="28" cy="28" r="28" fill="#B8643E"/>
          <rect x="16" y="14" width="24" height="3" rx="1.5" fill="#FBF6EE"/>
          <rect x="16" y="22" width="24" height="3" rx="1.5" fill="#FBF6EE"/>
          <rect x="16" y="30" width="24" height="3" rx="1.5" fill="#FBF6EE"/>
          <rect x="16" y="38" width="24" height="3" rx="1.5" fill="#FBF6EE"/>
        </svg>
      </div>
      <h1 class="login-title">Pantio</h1>
      <p class="login-sub">Your kitchen, organised.</p>
      <div class="login-actions">
        <PButton full-width @click="auth.login()">Sign in</PButton>
        <PButton variant="secondary" full-width @click="auth.signup()">Create account</PButton>
      </div>
    </div>
  </div>
</template>

<style scoped>
.login-page {
  min-height: 100dvh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--space-4);
  background: var(--bg);
}

.login-card {
  width: 100%;
  max-width: 360px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-4);
  text-align: center;
}

.login-mark {
  border-radius: 50%;
  overflow: hidden;
}

.login-title {
  font-family: 'Instrument Serif', Georgia, serif;
  font-size: 40px;
  font-weight: 400;
  font-style: italic;
  color: var(--fg);
}

.login-sub {
  color: var(--fg-muted);
  margin-top: calc(-1 * var(--space-2));
}

.login-actions {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  margin-top: var(--space-2);
}
</style>
