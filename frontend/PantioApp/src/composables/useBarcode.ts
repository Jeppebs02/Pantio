import { ref, nextTick } from 'vue'
import { Capacitor } from '@capacitor/core'
import { BarcodeScanner, SupportedFormat } from '@capacitor-community/barcode-scanner'
import type { BrowserMultiFormatReader as ZxingReader } from '@zxing/library'

const ERR_PERMISSION =
  'Kamera-adgang er nødvendig for at scanne stregkoder. Giv tilladelse i indstillinger.'
const ERR_SCAN_FAILED =
  'Scanning fejlede. Prøv igen eller indtast EAN manuelt.'

export function useBarcode() {
  const isScanning = ref(false)
  const error = ref('')

  let webReader: ZxingReader | null = null
  let webVideoEl: HTMLVideoElement | null = null

  async function ensurePermission(): Promise<boolean> {
    const status = await BarcodeScanner.checkPermission({ force: false })
    if (status.granted) return true
    if (status.denied || status.restricted) {
      error.value = ERR_PERMISSION
      return false
    }
    const requested = await BarcodeScanner.checkPermission({ force: true })
    if (requested.granted) return true
    error.value = ERR_PERMISSION
    return false
  }

  async function startScan(): Promise<string | null> {
    error.value = ''

    if (!Capacitor.isNativePlatform()) {
      isScanning.value = true
      await nextTick()
      return startWebScan()
    }

    if (!(await ensurePermission())) return null

    isScanning.value = true
    await nextTick()

    document.body.style.background = 'transparent'
    document.documentElement.style.background = 'transparent'
    const appEl = document.getElementById('app')
    if (appEl) appEl.style.visibility = 'hidden'

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

  async function startWebScan(): Promise<string | null> {
    const { BrowserMultiFormatReader, DecodeHintType, BarcodeFormat } = await import('@zxing/library')

    const hints = new Map([
      [DecodeHintType.POSSIBLE_FORMATS, [
        BarcodeFormat.EAN_13,
        BarcodeFormat.EAN_8,
        BarcodeFormat.UPC_A,
        BarcodeFormat.UPC_E,
        BarcodeFormat.CODE_128,
        BarcodeFormat.CODE_39,
      ]],
    ])

    webReader = new BrowserMultiFormatReader(hints)

    const video = document.createElement('video')
    video.setAttribute('playsinline', 'true')
    video.style.cssText =
      'position:fixed;inset:0;width:100%;height:100%;object-fit:cover;z-index:9998;'
    document.body.appendChild(video)
    webVideoEl = video

    try {
      const result = await webReader.decodeOnceFromConstraints(
        { video: { facingMode: { ideal: 'environment' } } },
        video,
      )
      return result.getText()
    } catch {
      // Thrown when reset() is called (cancel) or no barcode found
      return null
    } finally {
      cleanupWebScan()
      isScanning.value = false
    }
  }

  function cleanupWebScan(): void {
    webReader?.reset()
    webReader = null
    webVideoEl?.remove()
    webVideoEl = null
  }

  async function stopScan(): Promise<void> {
    if (!Capacitor.isNativePlatform()) {
      cleanupWebScan()
      isScanning.value = false
      return
    }

    try {
      await BarcodeScanner.stopScan()
      await BarcodeScanner.showBackground()
    } catch {
      // ignore teardown errors
    } finally {
      document.body.style.background = ''
      document.documentElement.style.background = ''
      const appEl = document.getElementById('app')
      if (appEl) appEl.style.visibility = ''
      isScanning.value = false
    }
  }

  return { isScanning, error, startScan, stopScan }
}
