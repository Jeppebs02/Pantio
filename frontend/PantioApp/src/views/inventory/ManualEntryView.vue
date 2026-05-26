<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Barcode, Camera, Search, UploadCloud } from 'lucide-vue-next'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import PButton from '../../components/ui/PButton.vue'
import PInput from '../../components/ui/PInput.vue'
import PAlert from '../../components/ui/PAlert.vue'
import BarcodeScannerOverlay from '../../components/BarcodeScanner.vue'
import { useInventoryStore } from '../../stores/inventory'
import { getProductByEan, contributeQuantity, contributeNewProduct, contributeNutritionImage } from '../../services/inventory'
import { ApiError } from '../../services/api'
import { useBarcode } from '../../composables/useBarcode'
import { useToast } from '../../composables/useToast'
import { Capacitor } from '@capacitor/core'
import type { QuantityUnit } from '../../services/types'

const route = useRoute()
const router = useRouter()
const store = useInventoryStore()

const inventoryId = route.params.id as string

const ean = ref('')
const eanError = ref('')

watch(ean, (val) => {
  const cleaned = val.replace(/\D/g, '').slice(0, 13)
  if (cleaned !== val) ean.value = cleaned
  if (eanError.value) eanError.value = ''
})
const productName = ref('')
const quantity = ref(1)
const quantityUnit = ref<QuantityUnit | null>(null)

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
const missingQuantity = ref(false)
const missingNutrition = ref(false)
const productNotFound = ref(false)
const nutritionContributed = ref(false)
const nutritionInput = ref<HTMLInputElement | null>(null)

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
  if (!/^\d{13}$/.test(ean.value.trim())) {
    eanError.value = 'EAN skal være præcis 13 cifre'
    return
  }
  eanError.value = ''
  isLookingUp.value = true
  error.value = ''
  lookupResult.value = null
  expirySource.value = null
  missingQuantity.value = false
  missingNutrition.value = false
  productNotFound.value = false
  nutritionContributed.value = false
  try {
    const product = await getProductByEan(ean.value.trim())
    productName.value = product.productName
    lookupResult.value = `Fundet: ${product.productName}`

    if (product.quantity != null) {
      quantity.value = product.quantity
    } else {
      missingQuantity.value = true
    }
    if (product.quantityUnit != null) quantityUnit.value = product.quantityUnit as QuantityUnit

    if (product.nutrition == null) missingNutrition.value = true

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
      productNotFound.value = true
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
      quantityUnit: quantityUnit.value,
      ean: ean.value.trim() || null,
      storageLocation: storageLocation.value.trim() || null,
      addedVia: ean.value.trim() ? 'Barcode' : 'Manual',
      manualExpiryDate: manualExpiryDate.value || null,
    })

    if (ean.value.trim()) {
      if (productNotFound.value) {
        contributeNewProduct(ean.value.trim(), productName.value.trim(), quantity.value, quantityUnit.value).catch(() => {})
      } else if (missingQuantity.value) {
        contributeQuantity(ean.value.trim(), quantity.value, quantityUnit.value).catch(() => {})
      }
    }

    toast.show(`${productName.value.trim()} tilføjet til lager`, 'success')
    router.back()
  } catch {
    toast.show('Kunne ikke gemme vare. Prøv igen.', 'error')
  } finally {
    isSaving.value = false
  }
}

function triggerNutritionPhoto() {
  nutritionInput.value?.click()
}

async function onNutritionPhoto(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file || !ean.value.trim()) return
  try {
    await contributeNutritionImage(ean.value.trim(), file)
    nutritionContributed.value = true
  } catch {
    toast.show('Kunne ikke sende billede. Prøv igen.', 'error')
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
            :maxlength="13"
            :error="eanError"
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
          <div class="unit-wrap">
            <label class="unit-label eyebrow">Enhed</label>
            <select v-model="quantityUnit" class="unit-select">
              <option :value="null">— stk —</option>
              <option value="l">l</option>
              <option value="dl">dl</option>
              <option value="cl">cl</option>
              <option value="ml">ml</option>
              <option value="kg">kg</option>
              <option value="g">g</option>
              <option value="mg">mg</option>
            </select>
          </div>
        </div>
        <p v-if="missingQuantity" class="off-hint">
          <UploadCloud :size="12" />
          Mængde mangler i OpenFoodFacts — din angivelse bidrager automatisk
        </p>

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

        <div v-if="(missingNutrition || productNotFound) && ean" class="nutrition-contribute">
          <div class="nutrition-contribute-header">
            <span class="field-label">Næringsindhold mangler i OpenFoodFacts</span>
          </div>
          <PButton
            v-if="!nutritionContributed"
            type="button"
            variant="secondary"
            size="sm"
            @click="triggerNutritionPhoto"
          >
            <Camera :size="16" />
            Tag billede af næringsindhold
          </PButton>
          <p v-else class="off-hint off-hint--success">
            <UploadCloud :size="12" />
            Billede sendt til OpenFoodFacts — tak for bidraget!
          </p>
          <input
            ref="nutritionInput"
            type="file"
            accept="image/*"
            capture="environment"
            hidden
            @change="onNutritionPhoto"
          />
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

.unit-wrap {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.unit-label {
  display: block;
}

.unit-select {
  width: 100%;
  padding: 10px 12px;
  border-radius: var(--radius-md);
  border: 1px solid var(--border);
  background: var(--surface);
  color: var(--fg);
  font-size: 15px;
  line-height: 24px;
  box-shadow: var(--shadow-sm);
  outline: none;
}

.unit-select:focus {
  border-color: var(--sage-600);
  box-shadow: 0 0 0 3px var(--sage-100);
}

.off-hint {
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 11px;
  color: var(--fg-faint);
  padding-left: 2px;
}

.off-hint--success {
  color: var(--sage-600);
}

.nutrition-contribute {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  padding: var(--space-3);
  background: var(--bg);
  border: 1px dashed var(--border);
  border-radius: var(--radius-md);
}

.nutrition-contribute-header {
  display: flex;
  align-items: center;
  gap: var(--space-2);
}
</style>
