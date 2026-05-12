<script setup lang="ts">
import { useRouter } from 'vue-router'
import { ArrowLeft } from 'lucide-vue-next'

const props = defineProps<{
  title: string
  eyebrow?: string
  backRoute?: string | object
}>()

const router = useRouter()

function goBack() {
  if (props.backRoute) {
    router.push(props.backRoute as string)
  } else {
    router.back()
  }
}
</script>

<template>
  <header class="topbar">
    <div class="topbar-lead">
      <button v-if="backRoute" class="topbar-back" aria-label="Go back" @click="goBack">
        <ArrowLeft :size="20" />
      </button>
      <div class="topbar-titles">
        <p v-if="eyebrow" class="eyebrow">{{ eyebrow }}</p>
        <h1 class="topbar-title">{{ title }}</h1>
      </div>
    </div>
    <div class="topbar-actions">
      <slot />
    </div>
  </header>
</template>

<style scoped>
.topbar {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 100;
  height: var(--topbar-height);
  background: var(--bg);
  border-bottom: 1px solid var(--border);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 var(--space-4);
  gap: var(--space-2);
}

.topbar-lead {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  min-width: 0;
}

.topbar-back {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border-radius: var(--radius-md);
  color: var(--fg-muted);
  flex-shrink: 0;
  transition: background var(--motion-default);
}

.topbar-back:hover {
  background: var(--surface-raised);
}

.topbar-titles {
  min-width: 0;
}

.topbar-title {
  font-size: 19px;
  line-height: 24px;
  font-weight: 700;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.eyebrow {
  margin-bottom: 1px;
}

.topbar-actions {
  display: flex;
  align-items: center;
  gap: var(--space-1);
  flex-shrink: 0;
}
</style>
