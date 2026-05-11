<script setup lang="ts">
import { Check } from 'lucide-vue-next'
import type { ShoppingListItemDto } from '../../services/types'

defineProps<{
  item: ShoppingListItemDto
}>()

const emit = defineEmits<{
  toggle: []
  delete: []
}>()
</script>

<template>
  <div class="shopping-item" :class="{ checked: item.isChecked }">
    <button
      class="shopping-check"
      :class="{ 'shopping-check--checked': item.isChecked }"
      :aria-label="item.isChecked ? 'Marker som ikke gjort' : 'Marker som gjort'"
      @click="emit('toggle')"
    >
      <Check v-if="item.isChecked" :size="14" />
    </button>
    <span class="shopping-name">{{ item.name }}</span>
    <span v-if="item.quantity" class="shopping-qty mono">
      {{ item.quantity }}{{ item.measuringUnit ? ' ' + item.measuringUnit : '' }}
    </span>
    <button class="shopping-delete" aria-label="Fjern vare" @click="emit('delete')">
      ×
    </button>
  </div>
</template>

<style scoped>
.shopping-item {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  padding: var(--space-3) var(--space-4);
  background: var(--surface);
  border-radius: var(--radius-md);
  border: 1px solid var(--border);
  transition: opacity var(--motion-default);
}

.shopping-item.checked {
  opacity: 0.55;
}

.shopping-check {
  width: 22px;
  height: 22px;
  border-radius: var(--radius-sm);
  border: 1.5px solid var(--border-strong);
  background: var(--surface);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: background var(--motion-default), border-color var(--motion-default);
  cursor: pointer;
}

.shopping-check--checked {
  background: var(--sage-600);
  border-color: var(--sage-600);
  color: #fff;
}

.shopping-name {
  flex: 1;
  font-size: 14px;
  color: var(--fg);
  text-decoration: v-bind("item.isChecked ? 'line-through' : 'none'");
}

.shopping-qty {
  color: var(--fg-faint);
  font-size: 12px;
}

.shopping-delete {
  color: var(--fg-faint);
  font-size: 18px;
  line-height: 1;
  padding: 0 var(--space-1);
  border-radius: var(--radius-sm);
  transition: color var(--motion-default), background var(--motion-default);
  cursor: pointer;
}

.shopping-delete:hover {
  color: var(--past);
  background: var(--past-bg);
}
</style>
