import { ref } from 'vue'

const visible = ref(false)
const message = ref('')
const variant = ref<'success' | 'error'>('success')

export function useToast() {
  function show(msg: string, v: 'success' | 'error' = 'success') {
    message.value = msg
    variant.value = v
    visible.value = true
  }

  return { visible, message, variant, show }
}
