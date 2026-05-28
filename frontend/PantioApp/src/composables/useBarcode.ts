import { ref, nextTick } from 'vue'
import { Capacitor } from '@capacitor/core'
import { BarcodeScanner, BarcodeFormat } from '@capacitor-mlkit/barcode-scanning'
import type { BrowserMultiFormatReader as ZxingReader } from '@zxing/library'

const ERR_PERMISSION =
  'Kamera-adgang er nødvendig for at scanne stregkoder. Giv tilladelse i indstillinger.'
const ERR_SCAN_FAILED =
  'Scanning fejlede. Prøv igen eller indtast EAN manuelt.'

const FORMATS = [
  BarcodeFormat.Ean13,
  BarcodeFormat.Ean8,
  BarcodeFormat.UpcA,
  BarcodeFormat.UpcE,
  BarcodeFormat.Code128,
  BarcodeFormat.Code39,
]

export function useBarcode() {
  const isScanning = ref(false)
  const error = ref('')

  let webReader: ZxingReader | null = null
  let webVideoEl: HTMLVideoElement | null = null

  async function ensurePermission(): Promise<boolean> {
    const { camera } = await BarcodeScanner.checkPermissions()
    if (camera === 'granted') return true
    if (camera === 'denied') {
      error.value = ERR_PERMISSION
      return false
    }
    const { camera: requested } = await BarcodeScanner.requestPermissions()
    if (requested === 'granted') return true
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

    try {
      const { barcodes } = await BarcodeScanner.scan({ formats: FORMATS })
      return barcodes[0]?.rawValue ?? null
    } catch {
      error.value = ERR_SCAN_FAILED
      return null
    }
  }

  async function startWebScan(): Promise<string | null> {
    const { BrowserMultiFormatReader, DecodeHintType, BarcodeFormat: ZxingFormat } = await import('@zxing/library')

    const hints = new Map([
      [DecodeHintType.POSSIBLE_FORMATS, [
        ZxingFormat.EAN_13,
        ZxingFormat.EAN_8,
        ZxingFormat.UPC_A,
        ZxingFormat.UPC_E,
        ZxingFormat.CODE_128,
        ZxingFormat.CODE_39,
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
        {
          video: {
            facingMode: { ideal: 'environment' },
            width: { ideal: 1920 },
            height: { ideal: 1080 },
          },
        },
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
    } catch {
      // ignore teardown errors
    }
  }

  return { isScanning, error, startScan, stopScan }
}
