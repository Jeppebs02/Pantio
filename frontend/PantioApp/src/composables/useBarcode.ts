import { ref } from 'vue'
import { BarcodeScanner, SupportedFormat } from '@capacitor-community/barcode-scanner'

const ERR_PERMISSION =
  'Kamera-adgang er nødvendig for at scanne stregkoder. Giv tilladelse i indstillinger.'
const ERR_SCAN_FAILED =
  'Scanning fejlede. Prøv igen eller indtast EAN manuelt.'

export function useBarcode() {
  const isScanning = ref(false)
  const error = ref('')

  async function ensurePermission(): Promise<boolean> {
    const status = await BarcodeScanner.checkPermission({ force: false })
    if (status.granted) return true
    if (status.denied || status.restricted) {
      error.value = ERR_PERMISSION
      return false
    }
    // neverAsked / unknown — trigger the native permission dialog once
    const requested = await BarcodeScanner.checkPermission({ force: true })
    if (requested.granted) return true
    error.value = ERR_PERMISSION
    return false
  }

  async function startScan(): Promise<string | null> {
    error.value = ''
    if (!(await ensurePermission())) return null

    isScanning.value = true
    // Transparent WebView: CSS side — must be set before hideBackground()
    document.body.style.background = 'transparent'
    document.documentElement.style.background = 'transparent'

    try {
      await BarcodeScanner.hideBackground()
      const result = await BarcodeScanner.startScan({
        targetedFormats: [
          SupportedFormat.EAN_13,
          SupportedFormat.EAN_8,
          SupportedFormat.UPC_A,
          SupportedFormat.UPC_E,
          SupportedFormat.CODE_128,
          SupportedFormat.CODE_39,
        ],
      })
      return result.hasContent ? result.content : null
    } catch {
      error.value = ERR_SCAN_FAILED
      return null
    } finally {
      await stopScan()
    }
  }

  async function stopScan(): Promise<void> {
    try {
      await BarcodeScanner.stopScan()
      await BarcodeScanner.showBackground()
    } catch {
      // ignore teardown errors
    } finally {
      document.body.style.background = ''
      document.documentElement.style.background = ''
      isScanning.value = false
    }
  }

  return { isScanning, error, startScan, stopScan }
}
