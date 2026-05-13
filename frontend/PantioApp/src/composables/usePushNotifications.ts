import { watch } from 'vue'
import { PushNotifications } from '@capacitor/push-notifications'
import { Capacitor } from '@capacitor/core'
import { useAuthStore } from '../stores/auth'
import { saveFcmToken } from '../services/users'
import { useToast } from './useToast'

export function usePushNotifications() {
  if (!Capacitor.isNativePlatform()) return

  const auth = useAuthStore()
  const toast = useToast()

  async function register() {
    const permission = await PushNotifications.requestPermissions()
    if (permission.receive !== 'granted') return

    await PushNotifications.register()
  }

  PushNotifications.addListener('registration', async ({ value: token }) => {
    if (auth.localUser) {
      try {
        await saveFcmToken(auth.localUser.id, token)
      } catch {
        // non-critical — notifications just won't arrive until next registration
      }
    }
  })

  PushNotifications.addListener('pushNotificationReceived', (notification) => {
    toast.show(notification.body ?? notification.title ?? 'Ny notifikation', 'success')
  })

  // Register as soon as the user is authenticated
  watch(
    () => auth.localUser,
    (user) => {
      if (user) register()
    },
    { immediate: true },
  )
}
