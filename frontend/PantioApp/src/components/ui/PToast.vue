<script setup lang="ts">
import { watch, onUnmounted } from 'vue'
import { CheckCircle2, XCircle } from 'lucide-vue-next'

const props = defineProps<{
  visible: boolean
  message: string
  variant?: 'success' | 'error'
  duration?: number
}>()

const emit = defineEmits<{
  'update:visible': [value: boolean]
}>()

let timer: ReturnType<typeof setTimeout> | null = null

watch(
  () => props.visible,
  (val) => {
    if (val) {
      if (timer) clearTimeout(timer)
      timer = setTimeout(() => emit('update:visible', false), props.duration ?? 3000)
    }
  },
)

onUnmounted(() => {
  if (timer) clearTimeout(timer)
})
</script>

<template>
  <Transition name="toast">
    <div
      v-if="visible"
      :class="['ptoast', `ptoast--${variant ?? 'success'}`]"
      role="status"
      aria-live="polite"
    >
      <CheckCircle2 v-if="(variant ?? 'success') === 'success'" :size="18" class="ptoast-icon" />
      <XCircle v-else :size="18" class="ptoast-icon" />
      <span>{{ message }}</span>
    </div>
  </Transition>
</template>

<style scoped>
.ptoast {
  position: fixed;
  bottom: calc(var(--space-6) + env(safe-area-inset-bottom, 0px));
  left: 50%;
  transform: translateX(-50%);
  z-index: 9999;
  display: flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-3) var(--space-5);
  border-radius: var(--radius-lg);
  font-size: 14px;
  font-weight: 500;
  white-space: nowrap;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.15);
  pointer-events: none;
}

.ptoast--success {
  background: var(--fresh);
  color: #fff;
}

.ptoast--error {
  background: var(--past);
  color: #fff;
}

.ptoast-icon {
  flex-shrink: 0;
}

.toast-enter-active,
.toast-leave-active {
  transition:
    opacity 0.25s ease,
    transform 0.25s ease;
}

.toast-enter-from,
.toast-leave-to {
  opacity: 0;
  transform: translateX(-50%) translateY(12px);
}
</style>
