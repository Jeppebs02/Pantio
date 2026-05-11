<script setup lang="ts">
import { ref, computed } from 'vue'
import { Archive, Receipt, ChefHat, Check } from 'lucide-vue-next'
import PButton from '../../components/ui/PButton.vue'
import { useAuthStore } from '../../stores/auth'

const auth = useAuthStore()
const step = ref(0)

const steps = [
  {
    icon: Archive,
    title: 'Welcome to Pantio',
    body: 'Track what\'s in your kitchen, get alerts before things expire, and waste less food.',
  },
  {
    icon: Receipt,
    title: 'Connect your store',
    body: 'Link your Netto account and Pantio will import your receipts automatically — no manual entry needed.',
  },
  {
    icon: ChefHat,
    title: 'Cook with what you have',
    body: 'Pick items that are almost expired and we\'ll suggest recipes that use them up.',
  },
  {
    icon: Check,
    title: 'You\'re all set',
    body: 'Create an account or sign in to get started.',
  },
]

const isLastStep = computed(() => step.value === steps.length - 1)

function next() {
  if (!isLastStep.value) step.value++
}
</script>

<template>
  <div class="onboarding">
    <div class="onboarding-card">
      <div class="onboarding-progress">
        <span
          v-for="(_, i) in steps"
          :key="i"
          class="progress-dot"
          :class="{ active: i === step, done: i < step }"
        />
      </div>

      <div class="onboarding-icon">
        <component :is="steps[step].icon" :size="40" />
      </div>

      <h1 class="onboarding-title">{{ steps[step].title }}</h1>
      <p class="onboarding-body">{{ steps[step].body }}</p>

      <template v-if="isLastStep">
        <PButton full-width @click="auth.signup()">Create account</PButton>
        <PButton variant="secondary" full-width @click="auth.login()">Sign in</PButton>
      </template>
      <template v-else>
        <PButton full-width @click="next">Continue</PButton>
        <button class="skip-btn" @click="step = steps.length - 1">Skip</button>
      </template>
    </div>
  </div>
</template>

<style scoped>
.onboarding {
  min-height: 100dvh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--space-4);
  background: var(--bg);
}

.onboarding-card {
  width: 100%;
  max-width: 400px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-5);
  text-align: center;
}

.onboarding-progress {
  display: flex;
  gap: var(--space-2);
}

.progress-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--border-strong);
  transition: background var(--motion-default), transform var(--motion-default);
}

.progress-dot.active {
  background: var(--sage-600);
  transform: scale(1.25);
}

.progress-dot.done {
  background: var(--sage-400);
}

.onboarding-icon {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  background: var(--sage-100);
  color: var(--sage-600);
  display: flex;
  align-items: center;
  justify-content: center;
}

.onboarding-title {
  font-size: 28px;
  font-weight: 700;
}

.onboarding-body {
  color: var(--fg-muted);
  max-width: 320px;
}

.skip-btn {
  font-size: 14px;
  color: var(--fg-faint);
  cursor: pointer;
  background: none;
  border: none;
  padding: var(--space-2);
  border-radius: var(--radius-sm);
  transition: color var(--motion-default);
}

.skip-btn:hover {
  color: var(--fg-muted);
}
</style>
