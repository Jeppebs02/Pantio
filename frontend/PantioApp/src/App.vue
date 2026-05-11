<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from './stores/auth'

const auth = useAuthStore()
const router = useRouter()

onMounted(async () => {
  await auth.initialize()

  if (auth.isAuthenticated && auth.localUser && !auth.localUser.onboardingDone) {
    router.replace('/onboarding')
  }
})
</script>

<template>
  <router-view />
</template>
