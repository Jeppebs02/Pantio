<script setup lang="ts">
import PToast from './components/ui/PToast.vue'
import PConfirm from './components/ui/PConfirm.vue'
import { useToast } from './composables/useToast'
import { usePushNotifications } from './composables/usePushNotifications'

const { visible, message, variant } = useToast()
usePushNotifications()
</script>

<template>
  <router-view />
  <PToast v-model:visible="visible" :message="message" :variant="variant" />
  <PConfirm />
</template>
