<script setup lang="ts">
import BottomNav from './BottomNav.vue'
</script>

<template>
  <div class="app-shell">
    <slot name="topbar" />
    <main class="app-content">
      <slot />
    </main>
    <BottomNav />
  </div>
</template>

<style scoped>
.app-shell {
  display: flex;
  flex-direction: column;
  min-height: 100%;
}

.app-content {
  flex: 1;
  padding-top: var(--topbar-height);
  padding-bottom: var(--bottomnav-height);
  overflow-y: auto;
}
</style>
