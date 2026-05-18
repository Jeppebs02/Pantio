import { ref } from 'vue'

export interface ConfirmOptions {
  title?: string
  confirmLabel?: string
  cancelLabel?: string
  danger?: boolean
}

const visible = ref(false)
const message = ref('')
const opts = ref<ConfirmOptions>({})
let resolveCallback: ((value: boolean) => void) | null = null

export function useConfirm() {
  function ask(msg: string, options: ConfirmOptions = {}): Promise<boolean> {
    message.value = msg
    opts.value = options
    visible.value = true
    return new Promise<boolean>((resolve) => {
      resolveCallback = resolve
    })
  }

  function respond(value: boolean) {
    visible.value = false
    resolveCallback?.(value)
    resolveCallback = null
  }

  return { visible, message, opts, ask, respond }
}
