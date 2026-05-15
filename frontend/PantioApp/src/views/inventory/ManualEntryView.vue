<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Barcode, Camera, Search } from 'lucide-vue-next'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import PButton from '../../components/ui/PButton.vue'
import PInput from '../../components/ui/PInput.vue'
import PAlert from '../../components/ui/PAlert.vue'
import BarcodeScannerOverlay from '../../components/BarcodeScanner.vue'
import { useInventoryStore } from '../../stores/inventory'
import { getProductByEan } from '../../services/inventory'
import { ApiError } from '../../services/api'
import { useBarcode } from '../../composables/useBarcode'
import { useToast } from '../../composables/useToast'
import { Capacitor } from '@capacitor/core'

const route = useRoute()
const router = useRouter()
const store = useInventoryStore()

const inventoryId = route.params.id as string

const ean = ref('')
const productName = ref('')
const quantity = ref(1)
const quantityUnit = ref('')

function clampQuantity() {
  if (!quantity.value || quantity.value < 1) quantity.value = 1
}
const storageLocation = ref('')
const manualExpiryDate = ref('')

const isLookingUp = ref(false)
const isSaving = ref(false)
const error = ref('')
const lookupResult = ref<string | null>(null)
const expirySource = ref<{ categoryName: string; days: number } | null>(null)

const { isScanning, error: scanError, startScan, stopScan } = useBarcode()
const toast = useToast()

onMounted(async () => {
  const prefilledEan = route.query.ean as string | undefined
  if (prefilledEan) {
    ean.value = prefilledEan
    await lookupEan()
  }
})

async function openScanner() {
  if (!Capacitor.isNativePlatform()) {
    const input = window.prompt('[DEV] Simuler scanning — indtast EAN:')
    if (input?.trim()) {
      ean.value = input.trim()
      await lookupEan()
    }
    return
  }
  const scanned = await startScan()
  if (scanned) {
    ean.value = scanned
    await lookupEan()
  }
}

async function lookupEan() {
  if (!ean.value.trim()) return
  isLookingUp.value = true
  error.value = ''
  lookupResult.value = null
  expirySource.value = null
  try {
    const product = await getProductByEan(ean.value.trim())
    productName.value = product.productName
    lookupResult.value = `Fundet: ${product.productName}`

    if (product.defaultShelfLifeDays && product.categoryName) {
      const d = new Date()
      d.setDate(d.getDate() + product.defaultShelfLifeDays)
      manualExpiryDate.value = d.toISOString().split('T')[0]
      expirySource.value = { categoryName: product.categoryName, days: product.defaultShelfLifeDays }
    }
  } catch (e) {
    if (e instanceof ApiError && e.status === 404) {
      lookupResult.value = 'Produkt ikke fundet — indtast navn manuelt.'
      productName.value = ''
      manualExpiryDate.value = ''
      expirySource.value = null
    } else {
      error.value = 'Opslag fejlede. Tjek stregkoden og prøv igen.'
    }
  } finally {
    isLookingUp.value = false
  }
}

async function save() {
  if (!productName.value.trim()) {
    error.value = 'Produktnavn er påkrævet.'
    return
  }
  isSaving.value = true
  error.value = ''
  try {
    await store.createItem(inventoryId, {
      productName: productName.value.trim(),
      quantity: quantity.value,
      quantityUnit: quantityUnit.value.trim() || null,
      ean: ean.value.trim() || null,
      storageLocation: storageLocation.value.trim() || null,
      addedVia: ean.value.trim() ? 'Barcode' : 'Manual',
      manualExpiryDate: manualExpiryDate.value || null,
    })
    toast.show(`${productName.value.trim()} tilføjet til lager`, 'success')
    router.back()
  } catch {
    toast.show('Kunne ikke gemme vare. Prøv igen.', 'error')
  } finally {
    isSaving.value = false
  }
}
</script>

<template>
  <AppShell>
    <template #topbar>
      <TopBar
        title="Tilføj vare"
        :back-route="{ name: 'inventory', params: { id: inventoryId } }"
      />
    </template>

    <div class="page">
      <PAlert v-if="error || scanError" variant="error">{{ error || scanError }}</PAlert>

      <div class="card">
        <h3>Stregkodeopslag</h3>
        <div class="ean-row">
          <PInput
            v-model="ean"
            placeholder="Indtast eller scan EAN"
            type="text"
          >
            <template #icon><Barcode :size="16" /></template>
          </PInput>
          <PButton variant="secondary" size="sm" :disabled="isLookingUp || !ean" @click="lookupEan">
            <Search :size="16" />
            {{ isLookingUp ? '...' : 'Slå op' }}
          </PButton>
          <PButton variant="ghost" size="sm" :disabled="isScanning" aria-label="Scan stregkode" @click="openScanner">
            <Camera :size="18" />
          </PButton>
        </div>

        <BarcodeScannerOverlay v-if="isScanning" @cancelled="stopScan" />
        <p v-if="lookupResult" class="lookup-result">{{ lookupResult }}</p>
        <p v-if="lookupResult?.startsWith('Fundet')" class="data-source">Produktdata hentet automatisk</p>
      </div>

      <form class="card form" @submit.prevent="save">
        <PInput v-model="productName" label="Produktnavn" placeholder="f.eks. Sødmælk" />

        <div class="quantity-field">
          <span class="field-label">Mængde</span>
          <div class="stepper">
            <button type="button" class="stepper-btn" :disabled="quantity <= 1" @click="quantity = Math.max(1, quantity - 1)">−</button>
            <input
              v-model.number="quantity"
              type="number"
              class="stepper-input"
              min="1"
              @input="clampQuantity"
              @paste="clampQuantity"
            />
            <button type="button" class="stepper-btn" @click="quantity++">+</button>
          </div>
        </div>

        <PInput v-model="quantityUnit" label="Enhed" placeholder="f.eks. L, g, stk" />

        <PInput v-model="storageLocation" label="Opbevaringssted (valgfrit)" placeholder="f.eks. Øverste hylde" />
        <div class="expiry-wrap">
          <PInput v-model="manualExpiryDate" label="Udløbsdato (valgfrit)" type="date" />
          <p v-if="expirySource" class="expiry-hint">
            Estimat baseret på kategori: {{ expirySource.categoryName }} ({{ expirySource.days }} dage)
          </p>
          <p v-else-if="lookupResult && !expirySource && productName" class="expiry-hint expiry-hint--manual">
            Ingen kategori fundet — udfyld dato manuelt
          </p>
        </div>

        <PButton type="submit" full-width :disabled="isSaving || !productName.trim()">
          {{ isSaving ? 'Gemmer...' : 'Tilføj til lager' }}
        </PButton>
      </form>
    </div>
  </AppShell>
</template>

<style scoped>
.page {
  padding: var(--space-4);
  max-width: var(--max-width);
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: var(--space-5);
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.ean-row {
  display: flex;
  gap: var(--space-2);
  align-items: flex-end;
}

.ean-row .pinput-wrap {
  flex: 1;
}

.lookup-result {
  font-size: 13px;
  color: var(--fg-muted);
}

.data-source {
  font-size: 11px;
  color: var(--fg-faint);
}

.expiry-wrap {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.expiry-hint {
  font-size: 12px;
  color: var(--fg-muted);
  padding-left: 2px;
}

.expiry-hint--manual {
  color: var(--soon);
}

.quantity-field {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.field-label {
  font-size: 13px;
  font-weight: 600;
  color: var(--fg-muted);
}

.stepper {
  display: flex;
  align-items: center;
  border: 1.5px solid var(--border-strong);
  border-radius: var(--radius-md);
  overflow: hidden;
  background: var(--surface);
}

.stepper-btn {
  width: 44px;
  height: 44px;
  font-size: 20px;
  font-weight: 400;
  color: var(--fg-muted);
  background: var(--bg);
  border: none;
  cursor: pointer;
  flex-shrink: 0;
  transition: background var(--motion-default), color var(--motion-default);
}

.stepper-btn:hover:not(:disabled) {
  background: var(--surface-raised);
  color: var(--fg);
}

.stepper-btn:disabled {
  opacity: 0.35;
  cursor: not-allowed;
}

.stepper-input {
  flex: 1;
  height: 44px;
  text-align: center;
  border: none;
  border-left: 1px solid var(--border);
  border-right: 1px solid var(--border);
  background: var(--surface);
  font-size: 16px;
  font-weight: 600;
  color: var(--fg);
  outline: none;
  min-width: 0;
}

.stepper-input::-webkit-inner-spin-button,
.stepper-input::-webkit-outer-spin-button {
  -webkit-appearance: none;
}
</style>
