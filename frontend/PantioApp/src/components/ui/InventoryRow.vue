<script setup lang="ts">
import { computed } from 'vue'
import { Package } from 'lucide-vue-next'
import PBadge from './PBadge.vue'
import type { InventoryItemDto } from '../../services/types'

const props = defineProps<{
  item: InventoryItemDto
}>()

const emit = defineEmits<{
  click: []
}>()

type BadgeTone = 'fresh' | 'soon' | 'past' | 'neutral'

const expiryLabel = computed(() => {
  const expiry = props.item.expiryDate
  if (!expiry) return null
  const date = new Date(expiry.estimatedExpiry)
  const now = new Date()
  const diffDays = Math.ceil((date.getTime() - now.getTime()) / (1000 * 60 * 60 * 24))
  if (diffDays < 0) return { label: 'Expired', tone: 'past' as BadgeTone }
  if (diffDays === 0) return { label: 'Today', tone: 'soon' as BadgeTone }
  if (diffDays === 1) return { label: 'Tomorrow', tone: 'soon' as BadgeTone }
  if (diffDays <= 3) return { label: `${diffDays} days`, tone: 'soon' as BadgeTone }
  if (diffDays <= 7) return { label: `${diffDays} days`, tone: 'fresh' as BadgeTone }
  return { label: new Date(expiry.estimatedExpiry).toLocaleDateString('da-DK', { day: 'numeric', month: 'short' }), tone: 'neutral' as BadgeTone }
})

const qtyLabel = computed(() => {
  const q = props.item.quantity
  const u = props.item.quantityUnit
  return u ? `${q} ${u}` : String(q)
})
</script>

<template>
  <button class="inv-row" @click="emit('click')">
    <span class="inv-row-thumb" aria-hidden="true">
      <Package :size="24" />
    </span>
    <span class="inv-row-meta">
      <span class="inv-row-name">{{ item.productName }}</span>
      <span class="inv-row-sub">{{ item.addedVia }}</span>
    </span>
    <span class="inv-row-right">
      <PBadge v-if="expiryLabel" :tone="expiryLabel.tone" :dot="expiryLabel.tone !== 'neutral'">
        {{ expiryLabel.label }}
      </PBadge>
      <span class="inv-row-qty mono">{{ qtyLabel }}</span>
    </span>
  </button>
</template>

<style scoped>
.inv-row {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  width: 100%;
  padding: var(--space-3) var(--space-4);
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  text-align: left;
  cursor: pointer;
  transition: box-shadow var(--motion-default), background var(--motion-default);
}

.inv-row:hover {
  box-shadow: var(--shadow-sm);
}

.inv-row-thumb {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 48px;
  height: 48px;
  border-radius: var(--radius-md);
  background: var(--surface-raised);
  color: var(--fg-muted);
  flex-shrink: 0;
}

.inv-row-meta {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.inv-row-name {
  font-size: 15px;
  font-weight: 600;
  color: var(--fg);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.inv-row-sub {
  font-size: 13px;
  color: var(--fg-muted);
}

.inv-row-right {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: var(--space-1);
  flex-shrink: 0;
}

.inv-row-qty {
  color: var(--fg-faint);
}
</style>
