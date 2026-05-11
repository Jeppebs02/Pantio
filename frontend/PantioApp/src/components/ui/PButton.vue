<script setup lang="ts">
defineProps<{
  variant?: 'primary' | 'accent' | 'secondary' | 'ghost' | 'danger'
  size?: 'md' | 'sm'
  disabled?: boolean
  type?: 'button' | 'submit' | 'reset'
  fullWidth?: boolean
}>()
</script>

<template>
  <button
    :class="['pbtn', `pbtn--${variant ?? 'primary'}`, `pbtn--${size ?? 'md'}`, { 'pbtn--full': fullWidth }]"
    :disabled="disabled"
    :type="type ?? 'button'"
  >
    <slot />
  </button>
</template>

<style scoped>
.pbtn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-2);
  font-family: inherit;
  font-weight: 600;
  border-radius: var(--radius-md);
  border: none;
  cursor: pointer;
  text-decoration: none;
  white-space: nowrap;
  transition: background var(--motion-default), color var(--motion-default), box-shadow var(--motion-default);
  box-shadow: var(--shadow-sm);
}

.pbtn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.pbtn:focus-visible {
  outline: 2px solid var(--sage-600);
  outline-offset: 2px;
}

.pbtn:active:not(:disabled) {
  transform: scale(0.98);
}

/* Sizes */
.pbtn--md {
  height: 44px;
  padding: 0 var(--space-5);
  font-size: 15px;
}

.pbtn--sm {
  height: 34px;
  padding: 0 var(--space-4);
  font-size: 13px;
}

.pbtn--full {
  width: 100%;
}

/* Variants */
.pbtn--primary {
  background: var(--sage-600);
  color: #fff;
}
.pbtn--primary:hover:not(:disabled) {
  background: var(--sage-700);
}

.pbtn--accent {
  background: var(--clay-600);
  color: #fff;
}
.pbtn--accent:hover:not(:disabled) {
  background: var(--clay-700);
}

.pbtn--secondary {
  background: var(--surface);
  color: var(--fg);
  border: 1px solid var(--border-strong);
}
.pbtn--secondary:hover:not(:disabled) {
  background: var(--surface-raised);
}

.pbtn--ghost {
  background: transparent;
  color: var(--fg-muted);
  box-shadow: none;
}
.pbtn--ghost:hover:not(:disabled) {
  background: var(--surface-raised);
  color: var(--fg);
}

.pbtn--danger {
  background: var(--past-bg);
  color: var(--past);
  border: 1px solid var(--past);
}
.pbtn--danger:hover:not(:disabled) {
  background: var(--past);
  color: #fff;
}
</style>
