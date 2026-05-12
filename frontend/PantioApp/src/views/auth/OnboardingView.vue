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
    title: 'Velkommen til Pantio',
    body: 'Hold styr på hvad der er i dit køkken, få besked inden det udløber, og spild mindre mad.',
  },
  {
    icon: Receipt,
    title: 'Tilslut din butik',
    body: 'Tilslut din Netto-konto, og Pantio importerer dine kvitteringer automatisk — ingen manuel indtastning.',
  },
  {
    icon: ChefHat,
    title: 'Lav mad med det du har',
    body: 'Vælg varer der snart udløber, og vi foreslår opskrifter der bruger dem op.',
  },
  {
    icon: Check,
    title: 'Du er klar',
    body: 'Opret en konto eller log ind for at komme i gang.',
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
        <PButton full-width @click="auth.signup()">Opret konto</PButton>
        <PButton variant="secondary" full-width @click="auth.login()">Log ind</PButton>
      </template>
      <template v-else>
        <PButton full-width @click="next">Fortsæt</PButton>
        <button class="skip-btn" @click="step = steps.length - 1">Spring over</button>
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
