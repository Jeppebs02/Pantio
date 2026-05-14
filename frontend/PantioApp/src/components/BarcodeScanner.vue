<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'
import { X } from 'lucide-vue-next'

const emit = defineEmits<{
  cancelled: []
}>()

onMounted(() => {
  document.body.style.overflow = 'hidden'
})
onUnmounted(() => {
  document.body.style.overflow = ''
})
</script>

<template>
  <!-- Teleport escapes AppShell/BottomNav stacking context -->
  <Teleport to="body">
    <div class="scanner-overlay" role="dialog" aria-modal="true" aria-label="Stregkodescanner">

      <div class="scanner-shade scanner-shade--top" />

      <div class="scanner-middle">
        <div class="scanner-shade scanner-shade--side" />

        <div class="scanner-viewfinder" aria-hidden="true">
          <span class="corner corner--tl" />
          <span class="corner corner--tr" />
          <span class="corner corner--bl" />
          <span class="corner corner--br" />
        </div>

        <div class="scanner-shade scanner-shade--side" />
      </div>

      <div class="scanner-shade scanner-shade--bottom">
        <p class="scanner-hint">Hold stregkoden inde i boksen</p>
        <button class="scanner-cancel" type="button" @click="emit('cancelled')">
          <X :size="20" />
          Annuller
        </button>
      </div>

    </div>
  </Teleport>
</template>

<style scoped>
.scanner-overlay {
  position: fixed;
  inset: 0;
  z-index: 9999;
  display: flex;
  flex-direction: column;
}

.scanner-shade {
  background: rgba(0, 0, 0, 0.62);
}

.scanner-shade--top {
  flex: 1;
  min-height: 120px;
}

.scanner-shade--bottom {
  flex: 1;
  min-height: 160px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: flex-start;
  padding-top: var(--space-5);
  gap: var(--space-5);
}

.scanner-shade--side {
  flex: 1;
}

.scanner-middle {
  display: flex;
  flex-direction: row;
  align-items: stretch;
  height: 240px;
  flex-shrink: 0;
}

/* Transparent gap — native camera shows through here */
.scanner-viewfinder {
  position: relative;
  width: 240px;
  flex-shrink: 0;
  background: transparent;
}

.corner {
  position: absolute;
  width: 24px;
  height: 24px;
  border-color: #ffffff;
  border-style: solid;
}

.corner--tl {
  top: 0;
  left: 0;
  border-width: 3px 0 0 3px;
  border-radius: var(--radius-sm) 0 0 0;
}

.corner--tr {
  top: 0;
  right: 0;
  border-width: 3px 3px 0 0;
  border-radius: 0 var(--radius-sm) 0 0;
}

.corner--bl {
  bottom: 0;
  left: 0;
  border-width: 0 0 3px 3px;
  border-radius: 0 0 0 var(--radius-sm);
}

.corner--br {
  bottom: 0;
  right: 0;
  border-width: 0 3px 3px 0;
  border-radius: 0 0 var(--radius-sm) 0;
}

.scanner-hint {
  color: rgba(255, 255, 255, 0.9);
  font-size: 14px;
  line-height: 20px;
  text-align: center;
  padding: 0 var(--space-6);
}

.scanner-cancel {
  display: inline-flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-3) var(--space-6);
  border-radius: var(--radius-full);
  background: rgba(255, 255, 255, 0.15);
  color: #ffffff;
  font-size: 15px;
  font-weight: 600;
  font-family: inherit;
  border: 1px solid rgba(255, 255, 255, 0.3);
  cursor: pointer;
  transition: background var(--motion-default);
}

.scanner-cancel:active {
  background: rgba(255, 255, 255, 0.28);
  transform: scale(0.97);
}
</style>
