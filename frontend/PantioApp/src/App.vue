<script setup lang="ts">
import PToast from './components/ui/PToast.vue'
import { useToast } from './composables/useToast'

const { visible, message, variant } = useToast()
</script>

<template>
  <router-view />
  <PToast v-model:visible="visible" :message="message" :variant="variant" />
</template>
