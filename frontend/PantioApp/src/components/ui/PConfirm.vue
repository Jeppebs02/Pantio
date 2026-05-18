<script setup lang="ts">
import PButton from './PButton.vue'
import { useConfirm } from '../../composables/useConfirm'

const { visible, message, opts, respond } = useConfirm()
</script>

<template>
  <Teleport to="body">
    <Transition name="confirm">
      <div v-if="visible" class="confirm-backdrop" @click.self="respond(false)">
        <div class="confirm-card" role="alertdialog" aria-modal="true">
          <p v-if="opts.title" class="confirm-title">{{ opts.title }}</p>
          <p class="confirm-message">{{ message }}</p>
          <div class="confirm-actions">
            <PButton variant="ghost" size="sm" @click="respond(false)">
              {{ opts.cancelLabel ?? 'Annuller' }}
            </PButton>
            <PButton :variant="opts.danger ? 'danger' : 'primary'" size="sm" @click="respond(true)">
              {{ opts.confirmLabel ?? 'Bekræft' }}
            </PButton>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.confirm-backdrop {
  position: fixed;
  inset: 0;
  z-index: 9000;
  background: rgba(0, 0, 0, 0.45);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--space-5);
}

.confirm-card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: var(--space-6);
  width: 100%;
  max-width: 320px;
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.18);
}

.confirm-title {
  font-size: 16px;
  font-weight: 700;
  color: var(--fg);
  margin: 0;
}

.confirm-message {
  font-size: 14px;
  color: var(--fg-muted);
  margin: 0;
  line-height: 1.5;
}

.confirm-actions {
  display: flex;
  gap: var(--space-2);
  justify-content: flex-end;
  margin-top: var(--space-1);
}

.confirm-enter-active {
  transition: opacity 0.2s ease;
}
.confirm-leave-active {
  transition: opacity 0.15s ease;
}
.confirm-enter-from,
.confirm-leave-to {
  opacity: 0;
}
.confirm-enter-active .confirm-card {
  animation: card-in 0.2s ease;
}
.confirm-leave-active .confirm-card {
  animation: card-out 0.15s ease forwards;
}

@keyframes card-in {
  from {
    transform: scale(0.95) translateY(6px);
    opacity: 0;
  }
  to {
    transform: scale(1) translateY(0);
    opacity: 1;
  }
}

@keyframes card-out {
  from { transform: scale(1); opacity: 1; }
  to   { transform: scale(0.95); opacity: 0; }
}
</style>
