<script setup lang="ts">
defineProps<{
  tone?: 'neutral' | 'fresh' | 'soon' | 'past' | 'clay' | 'info'
  dot?: boolean
}>()
</script>

<template>
  <span :class="['pbadge', `pbadge--${tone ?? 'neutral'}`]">
    <span v-if="dot" class="pbadge-dot" />
    <slot />
  </span>
</template>

<style scoped>
.pbadge {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-size: 12px;
  font-weight: 500;
  line-height: 16px;
  padding: 2px 8px;
  border-radius: var(--radius-full);
  white-space: nowrap;
}

.pbadge-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: currentColor;
  flex-shrink: 0;
}

.pbadge--neutral {
  background: var(--surface-raised);
  color: var(--fg-muted);
}

.pbadge--fresh {
  background: var(--fresh-bg);
  color: var(--fresh);
}

.pbadge--soon {
  background: var(--soon-bg);
  color: var(--soon);
}

.pbadge--past {
  background: var(--past-bg);
  color: var(--past);
}

.pbadge--clay {
  background: var(--clay-100);
  color: var(--clay-700);
}

.pbadge--info {
  background: var(--info-bg);
  color: var(--info);
}
</style>
