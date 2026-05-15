<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { Sparkles } from 'lucide-vue-next'

const messages = [
  'Analyserer dine ingredienser...',
  'Finder de bedste kombinationer...',
  'Skriver opskrifter til dig...',
  'Tilpasser portioner og mængder...',
  'Næsten klar...',
]

const progress = ref(0)
const messageIndex = ref(0)
const messageVisible = ref(true)

let progressTimer: ReturnType<typeof setInterval> | null = null
let messageTimer: ReturnType<typeof setInterval> | null = null

onMounted(() => {
  progressTimer = setInterval(() => {
    if (progress.value < 30) progress.value += 5
    else if (progress.value < 60) progress.value += 2
    else if (progress.value < 80) progress.value += 0.8
    else if (progress.value < 85) progress.value += 0.3
  }, 200)

  messageTimer = setInterval(() => {
    messageVisible.value = false
    setTimeout(() => {
      if (messageIndex.value < messages.length - 1) messageIndex.value++
      messageVisible.value = true
    }, 200)
  }, 1800)
})

onUnmounted(() => {
  if (progressTimer) clearInterval(progressTimer)
  if (messageTimer) clearInterval(messageTimer)
})
</script>

<template>
  <div class="loader">
    <div class="icon-wrap">
      <Sparkles :size="32" />
    </div>
    <p class="message" :class="{ visible: messageVisible }">
      {{ messages[messageIndex] }}
    </p>
    <div class="bar-track">
      <div class="bar-fill" :style="{ width: `${progress}%` }" />
    </div>
  </div>
</template>

<style scoped>
.loader {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-4);
  padding: var(--space-12) var(--space-4);
}

.icon-wrap {
  color: var(--sage-600);
  animation: spin 3s linear infinite;
}

.message {
  font-size: 14px;
  color: var(--fg-muted);
  opacity: 0;
  transition: opacity 200ms ease;
  text-align: center;
}

.message.visible {
  opacity: 1;
}

.bar-track {
  width: 100%;
  max-width: 280px;
  height: 6px;
  background: var(--border);
  border-radius: var(--radius-full);
  overflow: hidden;
}

.bar-fill {
  height: 100%;
  background: var(--sage-600);
  border-radius: var(--radius-full);
  transition: width 0.3s ease;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to   { transform: rotate(360deg); }
}
</style>
