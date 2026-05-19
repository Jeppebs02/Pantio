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
const quantity = ref('1')
const unit = ref<string | null>(null)
const isSaving = ref(false)

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
    const qty = parseFloat(quantity.value)
    await store.addItem(id, itemName.value.trim(), isNaN(qty) || qty <= 0 ? 1 : qty, unit.value)
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
        <div class="row">
          <PInput v-model="quantity" label="Antal" type="number" placeholder="1" />
          <div class="unit-wrap">
            <label class="unit-label eyebrow">Enhed</label>
            <select v-model="unit" class="unit-select">
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

.row {
  display: flex;
  gap: var(--space-3);
}

.row .pinput-wrap {
  flex: 1;
}

.row .unit-wrap {
  flex: 1;
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
</style>
