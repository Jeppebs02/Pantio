<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Barcode, Search } from 'lucide-vue-next'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import PButton from '../../components/ui/PButton.vue'
import PInput from '../../components/ui/PInput.vue'
import PAlert from '../../components/ui/PAlert.vue'
import { useInventoryStore } from '../../stores/inventory'
import { getProductByEan } from '../../services/inventory'
import { ApiError } from '../../services/api'

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

async function lookupEan() {
  if (!ean.value.trim()) return
  isLookingUp.value = true
  error.value = ''
  lookupResult.value = null
  try {
    const product = await getProductByEan(ean.value.trim())
    productName.value = product.productName
    lookupResult.value = `Found: ${product.productName}`
  } catch (e) {
    if (e instanceof ApiError && e.status === 404) {
      lookupResult.value = 'Product not found — enter name manually.'
    } else {
      error.value = 'Lookup failed. Check the barcode and try again.'
    }
  } finally {
    isLookingUp.value = false
  }
}

async function save() {
  if (!productName.value.trim()) {
    error.value = 'Product name is required.'
    return
  }
  const qty = parseFloat(quantity.value)
  if (isNaN(qty) || qty <= 0) {
    error.value = 'Enter a valid quantity.'
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
    error.value = 'Could not save item. Try again.'
  } finally {
    isSaving.value = false
  }
}
</script>

<template>
  <AppShell>
    <template #topbar>
      <TopBar
        title="Add item"
        :back-route="{ name: 'inventory', params: { id: inventoryId } }"
      />
    </template>

    <div class="page">
      <PAlert v-if="error" variant="error">{{ error }}</PAlert>

      <div class="card">
        <h3>Barcode lookup</h3>
        <div class="ean-row">
          <PInput
            v-model="ean"
            placeholder="Enter or scan EAN"
            type="text"
          >
            <template #icon><Barcode :size="16" /></template>
          </PInput>
          <PButton variant="secondary" size="sm" :disabled="isLookingUp || !ean" @click="lookupEan">
            <Search :size="16" />
            {{ isLookingUp ? '...' : 'Look up' }}
          </PButton>
        </div>
        <p v-if="lookupResult" class="lookup-result">{{ lookupResult }}</p>
      </div>

      <form class="card form" @submit.prevent="save">
        <PInput v-model="productName" label="Product name" placeholder="e.g. Whole milk" />

        <div class="form-row">
          <PInput v-model="quantity" label="Quantity" type="number" placeholder="1" />
          <PInput v-model="quantityUnit" label="Unit" placeholder="e.g. L, g, stk" />
        </div>

        <PInput v-model="storageLocation" label="Storage location (optional)" placeholder="e.g. Top shelf" />
        <PInput v-model="manualExpiryDate" label="Expiry date (optional)" type="date" />

        <PButton type="submit" full-width :disabled="isSaving || !productName.trim()">
          {{ isSaving ? 'Saving...' : 'Add to inventory' }}
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

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-3);
}
</style>
