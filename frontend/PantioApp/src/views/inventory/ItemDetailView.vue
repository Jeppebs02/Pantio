<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Trash2, CalendarClock, Pencil } from 'lucide-vue-next'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import PBadge from '../../components/ui/PBadge.vue'
import PButton from '../../components/ui/PButton.vue'
import PInput from '../../components/ui/PInput.vue'
import { useInventoryStore } from '../../stores/inventory'
import type { InventoryItemDto, QuantityUnit } from '../../services/types'
import { useConfirm } from '../../composables/useConfirm'

const route = useRoute()
const router = useRouter()
const store = useInventoryStore()

const inventoryId = route.params.id as string
const itemId = route.params.itemId as string

const item = computed<InventoryItemDto | undefined>(() => store.items.find((i) => i.id === itemId))

// ── Expiry edit ──
const editExpiry = ref(false)
const expiryInput = ref('')
const isSavingExpiry = ref(false)

// Always reflects the date we should display and pre-fill the edit form with
const effectiveExpiry = computed(() =>
  item.value?.expiryDate
    ? (item.value.expiryDate.overrideDate ?? item.value.expiryDate.estimatedExpiry)
    : '',
)

watch(effectiveExpiry, (val) => {
  if (val) expiryInput.value = val
}, { immediate: true })

// ── Details edit (name + quantity) ──
const editDetails = ref(false)
const nameInput = ref('')
const quantityInput = ref('')
const unitInput = ref<QuantityUnit | null>(null)
const isSavingDetails = ref(false)

watch(item, (val) => {
  if (val) {
    nameInput.value = val.productName
    quantityInput.value = String(val.quantity)
    unitInput.value = (val.quantityUnit as QuantityUnit | null) ?? null
  }
}, { immediate: true })

// ── Shared ──
const isDeleting = ref(false)
const { ask } = useConfirm()

onMounted(async () => {
  if (store.items.length === 0) {
    await store.fetchItems(inventoryId)
  }
})

async function deleteItem() {
  if (!await ask('Varen fjernes fra din beholdning.', { title: 'Fjern vare', confirmLabel: 'Fjern', danger: true })) return
  isDeleting.value = true
  try {
    await store.deleteItem(inventoryId, itemId)
    router.back()
  } finally {
    isDeleting.value = false
  }
}

async function saveExpiry() {
  if (!expiryInput.value) return
  isSavingExpiry.value = true
  try {
    await store.patchExpiry(inventoryId, itemId, expiryInput.value)
    editExpiry.value = false
  } finally {
    isSavingExpiry.value = false
  }
}

async function saveDetails() {
  if (!nameInput.value.trim()) return
  const qty = parseFloat(quantityInput.value)
  if (isNaN(qty) || qty <= 0) return
  isSavingDetails.value = true
  try {
    await store.updateItem(inventoryId, itemId, {
      productName: nameInput.value.trim(),
      quantity: qty,
      quantityUnit: unitInput.value,
      storageLocation: item.value?.storageLocation ?? null,
      status: item.value?.status ?? 'Available',
      rowVersion: item.value?.rowVersion ?? 0,
    })
    editDetails.value = false
  } finally {
    isSavingDetails.value = false
  }
}

function formatDate(d: string) {
  return new Date(d).toLocaleDateString('da-DK', { day: 'numeric', month: 'long', year: 'numeric' })
}

function daysUntil(d: string): string {
  const diff = Math.ceil((new Date(d).getTime() - Date.now()) / (1000 * 60 * 60 * 24))
  if (diff < 0) return `${Math.abs(diff)} dage siden`
  if (diff === 0) return 'I dag'
  if (diff === 1) return 'I morgen'
  return `Om ${diff} dage`
}

function expiryTone(d: string) {
  const diff = Math.ceil((new Date(d).getTime() - Date.now()) / (1000 * 60 * 60 * 24))
  if (diff < 0) return 'past'
  if (diff <= 3) return 'soon'
  return 'fresh'
}
</script>

<template>
  <AppShell>
    <template #topbar>
      <TopBar
        :title="item?.productName ?? 'Item'"
        :back-route="route.query.from === 'search' ? { name: 'search' } : { name: 'inventory', params: { id: inventoryId } }"
      >
        <button class="icon-btn danger" :disabled="isDeleting" aria-label="Slet vare" @click="deleteItem">
          <Trash2 :size="20" />
        </button>
      </TopBar>
    </template>

    <div v-if="item" class="page">
      <!-- Header / details card -->
      <div class="card">
        <div class="card-header">
          <div class="card-header-lead">
            <Pencil :size="16" />
            <h3>Detaljer</h3>
          </div>
          <button class="text-btn" @click="editDetails = !editDetails">
            {{ editDetails ? 'Annuller' : 'Rediger' }}
          </button>
        </div>

        <template v-if="!editDetails">
          <div class="item-header-row">
            <div>
              <h2>{{ item.productName }}</h2>
            </div>
            <PBadge :tone="item.status === 'Expired' ? 'past' : item.status === 'Low' ? 'soon' : 'fresh'">
              {{ item.status }}
            </PBadge>
          </div>
          <p class="qty-display">
            <span class="mono">{{ item.quantity }}</span>
            <span class="qty-unit">{{ item.quantityUnit ?? 'stk' }}</span>
          </p>
        </template>

        <form v-else class="details-form" @submit.prevent="saveDetails">
          <PInput v-model="nameInput" label="Produktnavn" placeholder="f.eks. Sødmælk" />
          <div class="form-row">
            <PInput v-model="quantityInput" label="Mængde" type="number" placeholder="1" />
            <div class="unit-wrap">
              <label class="unit-label eyebrow">Enhed</label>
              <select v-model="unitInput" class="unit-select">
                <option :value="null">— stk —</option>
                <option value="L">l</option>
                <option value="Dl">dl</option>
                <option value="Cl">cl</option>
                <option value="Ml">ml</option>
                <option value="Kg">kg</option>
                <option value="G">g</option>
                <option value="Mg">mg</option>
              </select>
            </div>
          </div>
          <PButton type="submit" size="sm" :disabled="isSavingDetails || !nameInput.trim()">
            {{ isSavingDetails ? 'Gemmer...' : 'Gem' }}
          </PButton>
        </form>
      </div>

      <!-- Expiry -->
      <div v-if="item.expiryDate" class="card">
        <div class="card-header">
          <div class="card-header-lead">
            <CalendarClock :size="18" />
            <h3>Udløb</h3>
          </div>
          <button class="text-btn" @click="editExpiry = !editExpiry">
            {{ editExpiry ? 'Annuller' : 'Rediger' }}
          </button>
        </div>

        <div v-if="!editExpiry" class="expiry-info">
          <PBadge :tone="expiryTone(effectiveExpiry)" :dot="true">
            {{ daysUntil(effectiveExpiry) }}
          </PBadge>
          <p class="expiry-date">{{ formatDate(effectiveExpiry) }}</p>
          <p v-if="item.expiryDate.isManualOverride" class="expiry-note">Manuel tilsidesættelse</p>
        </div>

        <form v-else class="expiry-form" @submit.prevent="saveExpiry">
          <PInput v-model="expiryInput" label="Udløbsdato" type="date" />
          <PButton type="submit" size="sm" :disabled="isSavingExpiry">
            {{ isSavingExpiry ? 'Gemmer...' : 'Gem' }}
          </PButton>
        </form>
      </div>

      <!-- Nutrition -->
      <div v-if="item.nutritionFacts" class="card">
        <h3>Næring per 100g</h3>
        <table class="nutrition-table">
          <tbody>
            <tr v-if="item.nutritionFacts.energyKcal100g !== null">
              <td>Energi</td>
              <td class="mono">{{ item.nutritionFacts.energyKcal100g }} kcal</td>
            </tr>
            <tr v-if="item.nutritionFacts.carbohydrates100g !== null">
              <td>Kulhydrater</td>
              <td class="mono">{{ item.nutritionFacts.carbohydrates100g }} g</td>
            </tr>
            <tr v-if="item.nutritionFacts.fat100g !== null">
              <td>Fedt</td>
              <td class="mono">{{ item.nutritionFacts.fat100g }} g</td>
            </tr>
            <tr v-if="item.nutritionFacts.proteins100g !== null">
              <td>Protein</td>
              <td class="mono">{{ item.nutritionFacts.proteins100g }} g</td>
            </tr>
            <tr v-if="item.nutritionFacts.salt100g !== null">
              <td>Salt</td>
              <td class="mono">{{ item.nutritionFacts.salt100g }} g</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Meta -->
      <div class="card meta-card">
        <dl>
          <div v-if="item.ean">
            <dt>Stregkode</dt>
            <dd class="mono">{{ item.ean }}</dd>
          </div>
          <div v-if="item.storageLocation">
            <dt>Placering</dt>
            <dd>{{ item.storageLocation }}</dd>
          </div>
          <div>
            <dt>Tilføjet</dt>
            <dd>{{ formatDate(item.addedAt) }}</dd>
          </div>
        </dl>
      </div>

      <PButton variant="danger" full-width :disabled="isDeleting" @click="deleteItem">
        <Trash2 :size="16" />
        {{ isDeleting ? 'Fjerner...' : 'Fjern vare' }}
      </PButton>
    </div>

    <div v-else class="page loading-state">
      <div v-for="i in 3" :key="i" class="skeleton-card" />
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
  gap: var(--space-3);
}

.card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: var(--space-5);
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.card-header-lead {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  color: var(--fg-muted);
}

.item-header-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--space-3);
}

.qty-display {
  display: flex;
  align-items: baseline;
  gap: var(--space-2);
}

.qty-display .mono {
  font-size: 36px;
  color: var(--fg);
}

.qty-unit {
  font-size: 16px;
  color: var(--fg-muted);
}

.details-form {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-3);
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

.text-btn {
  font-size: 13px;
  color: var(--sage-600);
  cursor: pointer;
  background: none;
  border: none;
  padding: var(--space-1) var(--space-2);
  border-radius: var(--radius-sm);
}

.expiry-info {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.expiry-date {
  font-size: 15px;
  color: var(--fg);
}

.expiry-note {
  font-size: 13px;
  color: var(--fg-faint);
}

.expiry-form {
  display: flex;
  gap: var(--space-2);
  align-items: flex-end;
}

.nutrition-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 14px;
}

.nutrition-table td {
  padding: var(--space-2) 0;
  border-bottom: 1px solid var(--border);
}

.nutrition-table td:last-child {
  text-align: right;
  color: var(--fg-muted);
}

.nutrition-table tr:last-child td {
  border-bottom: none;
}

.meta-card dl > div {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--space-2) 0;
  border-bottom: 1px solid var(--border);
  font-size: 14px;
}

.meta-card dl > div:last-child {
  border-bottom: none;
}

.meta-card dt {
  color: var(--fg-muted);
}

.icon-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border-radius: var(--radius-md);
  color: var(--fg-muted);
  transition: background var(--motion-default), color var(--motion-default);
  cursor: pointer;
  background: none;
  border: none;
}

.icon-btn.danger:hover {
  color: var(--past);
  background: var(--past-bg);
}

.loading-state {
  padding-top: var(--space-4);
}

.skeleton-card {
  height: 120px;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  animation: pulse 1.2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 0.4; }
  50% { opacity: 0.7; }
}
</style>
