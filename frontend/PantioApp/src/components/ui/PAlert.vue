<script setup lang="ts">
import { CheckCircle2, AlertTriangle, XCircle } from 'lucide-vue-next'

const props = defineProps<{
  variant?: 'info' | 'warning' | 'error'
  title?: string
}>()

const icons = { info: CheckCircle2, warning: AlertTriangle, error: XCircle }
const icon = () => icons[props.variant ?? 'info']
</script>

<template>
  <div :class="['palert', `palert--${variant ?? 'info'}`]" role="alert">
    <component :is="icon()" :size="20" class="palert-icon" />
    <div class="palert-body">
      <p v-if="title" class="palert-title">{{ title }}</p>
      <slot />
    </div>
  </div>
</template>

<style scoped>
.palert {
  display: flex;
  gap: var(--space-3);
  padding: var(--space-3) var(--space-4);
  border-radius: var(--radius-md);
  border: 1px solid transparent;
}

.palert-icon {
  flex-shrink: 0;
  margin-top: 2px;
}

.palert-body {
  flex: 1;
  font-size: 14px;
  line-height: 20px;
}

.palert-title {
  font-weight: 600;
  margin-bottom: 2px;
}

.palert--info {
  background: var(--fresh-bg);
  color: var(--fresh);
  border-color: var(--sage-200);
}

.palert--warning {
  background: var(--soon-bg);
  color: var(--soon);
  border-color: #e8c97c;
}

.palert--error {
  background: var(--past-bg);
  color: var(--past);
  border-color: #d9998a;
}
</style>
