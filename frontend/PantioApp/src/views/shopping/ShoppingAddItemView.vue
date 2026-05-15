<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import PButton from '../../components/ui/PButton.vue'
import PInput from '../../components/ui/PInput.vue'
import { useShoppingListStore } from '../../stores/shoppingList'

const route = useRoute()
const router = useRouter()
const store = useShoppingListStore()

const id = route.params.id as string

const itemName = ref('')
const quantity = ref(1)
const unit = ref('stk')
const isSaving = ref(false)

function clampQuantity() {
  if (!quantity.value || quantity.value < 1) quantity.value = 1
}

onMounted(async () => {
  if (store.lists.length === 0) {
    await store.fetchLists()
  }
  store.selectList(id)
})

async function save() {
  if (!itemName.value.trim()) return
  isSaving.value = true
  try {
    await store.addItem(id, itemName.value.trim(), quantity.value, unit.value.trim() || 'stk')
    router.replace({ name: 'shopping-detail', params: { id } })
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
        :back-route="{ name: 'shopping-detail', params: { id } }"
      />
    </template>

    <div class="page">
      <form class="form-card" @submit.prevent="save">
        <PInput v-model="itemName" label="Varenavn" placeholder="f.eks. Mælk" />

        <div class="quantity-field">
          <span class="field-label">Antal</span>
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

        <PInput v-model="unit" label="Enhed" placeholder="stk, kg, L..." />
        <PButton type="submit" :disabled="isSaving || !itemName.trim()">
          Tilføj vare
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
}

.form-card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: var(--space-4);
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
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
  gap: 0;
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
