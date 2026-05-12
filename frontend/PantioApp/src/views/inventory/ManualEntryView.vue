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
import { Capacitor } from '@capacitor/core'

const route = useRoute()
const router = useRouter()
const store = useInventoryStore()

const inventoryId = route.params.id as string

const ean = ref('')
const productName = ref('')
const quantity = ref('1')
const quantityUnit = ref('')
const storageLocation = ref('')
const manualExpiryDate = ref('')

const isLookingUp = ref(false)
const isSaving = ref(false)
const error = ref('')
const lookupResult = ref<string | null>(null)
const expirySource = ref<{ categoryName: string; days: number } | null>(null)

const { isScanning, error: scanError, startScan, stopScan } = useBarcode()

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
  const qty = parseFloat(quantity.value)
  if (isNaN(qty) || qty <= 0) {
    error.value = 'Indtast en gyldig mængde.'
    return
  }

  isSaving.value = true
  error.value = ''
  try {
    await store.createItem(inventoryId, {
      productName: productName.value.trim(),
      quantity: qty,
      quantityUnit: quantityUnit.value.trim() || null,
      ean: ean.value.trim() || null,
      storageLocation: storageLocation.value.trim() || null,
      addedVia: ean.value.trim() ? 'Barcode' : 'Manual',
      manualExpiryDate: manualExpiryDate.value || null,
    })
    router.back()
  } catch {
    error.value = 'Kunne ikke gemme vare. Prøv igen.'
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

        <div class="form-row">
          <PInput v-model="quantity" label="Mængde" type="number" placeholder="1" />
          <PInput v-model="quantityUnit" label="Enhed" placeholder="f.eks. L, g, stk" />
        </div>

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

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-3);
}
</style>
