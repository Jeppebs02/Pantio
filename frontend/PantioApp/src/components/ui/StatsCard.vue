<script setup lang="ts">
defineProps<{
  eyebrow: string
  value: string | number
  sub?: string
  progress?: number
}>()
</script>

<template>
  <div class="stats-card">
    <p class="eyebrow">{{ eyebrow }}</p>
    <p class="stats-value">{{ value }}</p>
    <p v-if="sub" class="stats-sub">{{ sub }}</p>
    <div v-if="progress !== undefined" class="stats-bar">
      <div class="stats-bar-fill" :style="{ width: `${Math.min(100, Math.max(0, progress))}%` }" />
    </div>
  </div>
</template>

<style scoped>
.stats-card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: var(--space-5);
}

.stats-value {
  font-family: 'Instrument Serif', Georgia, serif;
  font-size: 44px;
  line-height: 50px;
  font-weight: 400;
  color: var(--fg);
  margin-top: var(--space-1);
}

.stats-sub {
  font-size: 13px;
  color: var(--fg-muted);
  margin-top: var(--space-1);
}

.stats-bar {
  height: 6px;
  background: var(--border);
  border-radius: var(--radius-full);
  overflow: hidden;
  margin-top: var(--space-3);
}

.stats-bar-fill {
  height: 100%;
  background: var(--sage-600);
  border-radius: var(--radius-full);
  transition: width var(--motion-default);
}
</style>
