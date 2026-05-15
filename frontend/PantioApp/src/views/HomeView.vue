<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Archive, ShoppingCart, ChefHat, Receipt, User, ChevronRight, ScanBarcode, X } from 'lucide-vue-next'
import AppShell from '../components/layout/AppShell.vue'
import PAlert from '../components/ui/PAlert.vue'
import BarcodeScannerOverlay from '../components/BarcodeScanner.vue'
import { useBarcode } from '../composables/useBarcode'
import { useInventoryStore } from '../stores/inventory'
import { useStoreConnectionStore } from '../stores/storeConnection'
import { Capacitor } from '@capacitor/core'

const router = useRouter()
const inventoryStore = useInventoryStore()
const storeConnectionStore = useStoreConnectionStore()

onMounted(() => {
  storeConnectionStore.fetchConnections()
})
const { isScanning, error: scanError, startScan, stopScan } = useBarcode()

const showInventoryPicker = ref(false)
const pendingEan = ref('')

async function quickScan() {
  // Ensure inventories are loaded
  if (inventoryStore.inventories.length === 0) {
    await inventoryStore.fetchInventories()
  }

  // Dev fallback on web
  let scanned: string | null = null
  if (!Capacitor.isNativePlatform()) {
    const input = window.prompt('[DEV] Simuler scanning — indtast EAN:')
    scanned = input?.trim() || null
  } else {
    scanned = await startScan()
  }

  if (!scanned) return

  const inventories = inventoryStore.inventories
  if (inventories.length === 0) {
    router.push({ name: 'inventory-list' })
    return
  }

  if (inventories.length === 1) {
    router.push({ name: 'manual-entry', params: { id: inventories[0].id }, query: { ean: scanned } })
    return
  }

  // Multiple inventories — let user pick
  pendingEan.value = scanned
  showInventoryPicker.value = true
}

function selectInventory(inventoryId: string) {
  showInventoryPicker.value = false
  router.push({ name: 'manual-entry', params: { id: inventoryId }, query: { ean: pendingEan.value } })
  pendingEan.value = ''
}
</script>

<template>
  <AppShell>
    <div class="home">
      <!-- Brand header -->
      <header class="brand">
        <div class="brand-logo" aria-hidden="true">
          <svg xmlns="http://www.w3.org/2000/svg" width="52" height="52" viewBox="0 0 120 120" fill="none">
            <circle cx="60" cy="60" r="60" fill="#B8643E" />
            <g stroke="#FBF6EE" stroke-width="6" stroke-linecap="round" stroke-linejoin="round" fill="none">
              <path d="M40 28 L40 96" />
              <path d="M40 28 L74 28 Q88 28 88 42 Q88 56 74 56 L40 56" />
              <circle cx="56" cy="74" r="3.5" fill="#FBF6EE" />
              <circle cx="68" cy="74" r="3.5" fill="#FBF6EE" />
              <circle cx="80" cy="74" r="3.5" fill="#FBF6EE" />
              <path d="M52 86 L88 86" />
            </g>
          </svg>
        </div>
        <h1 class="brand-name">Pantio</h1>
        <p class="brand-tagline">Dit køkken, organiseret.</p>
      </header>

      <!-- Feature cards -->
      <nav class="menu">
        <!-- Quick scan -->
        <button class="card scan-btn" @click="quickScan" :disabled="isScanning">
          <span class="icon-badge icon-badge--neutral"><ScanBarcode :size="22" /></span>
          <div class="tile-text">
            <h3 class="card-title">Hurtig scan</h3>
            <p class="card-desc">Scan en stregkode og tilføj til lager</p>
          </div>
        </button>

        <!-- Hero: Lager -->
        <button class="card hero" @click="router.push({ name: 'inventory-list' })">
          <div class="hero-left">
            <span class="icon-badge icon-badge--neutral">
              <Archive :size="24" />
            </span>
            <div>
              <h2 class="card-title hero-title">Beholdning</h2>
              <p class="card-desc">Dine madvarer og udløbsdatoer</p>
            </div>
          </div>
          <ChevronRight :size="18" class="chevron" />
        </button>

        <!-- 2×2 grid -->
        <div class="grid">
          <button class="card tile tile--neutral" @click="router.push('/shopping')">
            <span class="icon-badge icon-badge--neutral"><ShoppingCart :size="20" /></span>
            <div class="tile-text">
              <h3 class="card-title">Indkøbsliste</h3>
              <p class="card-desc">Planlæg dine indkøb</p>
            </div>
          </button>

          <button class="card tile tile--neutral" @click="router.push('/recipes')">
            <span class="icon-badge icon-badge--neutral"><ChefHat :size="20" /></span>
            <div class="tile-text">
              <h3 class="card-title">Opskrifter</h3>
              <p class="card-desc">Find madopskrifter</p>
            </div>
          </button>

          <button class="card tile tile--neutral" @click="router.push('/store')">
            <span class="icon-badge icon-badge--neutral"><Receipt :size="20" /></span>
            <div class="tile-text">
              <h3 class="card-title">
                Netto+
                <span class="status-dot" :class="`status-dot--${storeConnectionStore.nettoStatus}`" />
              </h3>
              <p class="card-desc">
                {{ storeConnectionStore.nettoStatus === 'active' ? 'Netto+ forbundet' : 'Ikke forbundet' }}
              </p>
            </div>
          </button>

          <button class="card tile tile--neutral" @click="router.push('/settings')">
            <span class="icon-badge icon-badge--neutral"><User :size="20" /></span>
            <div class="tile-text">
              <h3 class="card-title">Dig</h3>
              <p class="card-desc">Konto og indstillinger</p>
            </div>
          </button>
        </div>
      </nav>

      <PAlert v-if="scanError" variant="error" class="scan-error">{{ scanError }}</PAlert>
    </div>

    <BarcodeScannerOverlay v-if="isScanning" @cancelled="stopScan" />

    <!-- Inventory picker bottom sheet -->
    <Teleport to="body">
      <div v-if="showInventoryPicker" class="picker-backdrop" @click.self="showInventoryPicker = false">
        <div class="picker-sheet">
          <div class="picker-header">
            <span class="picker-title">Vælg lager</span>
            <button class="picker-close" @click="showInventoryPicker = false"><X :size="20" /></button>
          </div>
          <ul class="picker-list">
            <li v-for="inv in inventoryStore.inventories" :key="inv.id">
              <button class="picker-item" @click="selectInventory(inv.id)">
                <Archive :size="18" />
                {{ inv.name }}
              </button>
            </li>
          </ul>
        </div>
      </div>
    </Teleport>
  </AppShell>
</template>

<style scoped>
/* ── Page ── */
.home {
  min-height: 100dvh;
  padding: 0 var(--space-4) var(--space-6);
  /* Cancel the forced topbar padding from AppShell (no TopBar on this page) */
  margin: calc(-1 * var(--topbar-height)) auto 0;
  display: flex;
  flex-direction: column;
  gap: var(--space-5);
  max-width: var(--max-width);
}

/* ── Brand header ── */
.brand {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-3);
  padding-top: var(--space-10);
  padding-bottom: var(--space-4);
  text-align: center;
  animation: fade-up 0.45s ease-out both;
}

.brand-logo {
  border-radius: 50%;
  overflow: hidden;
  box-shadow: var(--shadow-md);
}

.brand-name {
  font-family: 'Instrument Serif', Georgia, serif;
  font-size: 38px;
  font-weight: 400;
  font-style: italic;
  color: var(--fg);
  line-height: 1;
}

.brand-tagline {
  font-size: 14px;
  color: var(--fg-muted);
  margin-top: calc(-1 * var(--space-1));
}

/* ── Menu ── */
.menu {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

/* ── Cards base ── */
.card {
  display: flex;
  text-align: left;
  cursor: pointer;
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  background: var(--surface);
  transition:
    box-shadow var(--motion-default),
    transform var(--motion-default),
    background var(--motion-default),
    border-color var(--motion-default);
  -webkit-tap-highlight-color: transparent;
}

.card:hover {
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.card:active {
  transform: scale(0.985) translateY(0);
  box-shadow: var(--shadow-sm);
}

/* ── Hero card ── */
.hero {
  align-items: center;
  justify-content: space-between;
  padding: var(--space-4) var(--space-5);
  gap: var(--space-4);
  background: var(--surface-raised);
  border-color: var(--border-strong);
  animation: fade-up 0.45s ease-out 0.07s both;
}

.hero:hover {
  background: var(--surface);
  border-color: var(--border-strong);
}

.hero-left {
  display: flex;
  align-items: center;
  gap: var(--space-4);
}

.hero-title {
  font-size: 18px !important;
}

.chevron {
  color: var(--fg-muted);
  flex-shrink: 0;
}

/* ── Grid ── */
.grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-3);
  animation: fade-up 0.45s ease-out 0.14s both;
}

/* ── Tile cards ── */
.tile {
  flex-direction: column;
  align-items: flex-start;
  padding: var(--space-4) var(--space-4) var(--space-5);
  gap: var(--space-3);
  min-height: 148px;
}

.tile-text {
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.tile--clay {
  background: var(--clay-50);
  border-color: var(--clay-200);
}
.tile--clay:hover {
  background: var(--clay-100);
  border-color: var(--clay-300);
}

.tile--sage {
  background: var(--sage-50);
  border-color: var(--sage-200);
}
.tile--sage:hover {
  background: var(--sage-100);
  border-color: var(--sage-300);
}

.tile--neutral {
  background: var(--surface-raised);
  border-color: var(--border-strong);
}
.tile--neutral:hover {
  background: var(--surface);
}

/* ── Icon badges ── */
.icon-badge {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 44px;
  height: 44px;
  border-radius: var(--radius-md);
  flex-shrink: 0;
}

.icon-badge--sage {
  background: var(--sage-100);
  color: var(--sage-700);
}

.icon-badge--clay {
  background: var(--clay-100);
  color: var(--clay-700);
}

.icon-badge--neutral {
  background: var(--border);
  color: var(--fg-muted);
}

/* ── Card typography ── */
.card-title {
  font-size: 15px;
  font-weight: 700;
  color: var(--fg);
  line-height: 1.2;
  display: flex;
  align-items: center;
  gap: var(--space-2);
}

.status-dot {
  display: inline-block;
  width: 8px;
  height: 8px;
  border-radius: var(--radius-full);
  flex-shrink: 0;
}
.status-dot--active     { background: var(--fresh); }
.status-dot--pending    { background: var(--soon); }
.status-dot--disconnected { background: var(--past); }

.card-desc {
  font-size: 12px;
  color: var(--fg-muted);
  line-height: 1.4;
}

/* ── Quick scan button ── */
.scan-btn {
  flex-direction: row;
  align-items: center;
  padding: var(--space-4) var(--space-5);
  gap: var(--space-4);
  background: var(--surface-raised);
  border-color: var(--border-strong);
  animation: fade-up 0.45s ease-out 0.04s both;
}
.scan-btn:hover {
  background: var(--surface);
  border-color: var(--border-strong);
}
.scan-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.scan-error {
  animation: fade-up 0.3s ease-out both;
}

/* ── Inventory picker ── */
.picker-backdrop {
  position: fixed;
  inset: 0;
  z-index: 9998;
  background: rgba(0, 0, 0, 0.4);
  display: flex;
  align-items: flex-end;
}

.picker-sheet {
  width: 100%;
  background: var(--surface);
  border-radius: var(--radius-xl) var(--radius-xl) 0 0;
  padding: var(--space-5);
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
  box-shadow: var(--shadow-md);
}

.picker-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.picker-title {
  font-size: 16px;
  font-weight: 700;
  color: var(--fg);
}

.picker-close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border-radius: var(--radius-full);
  background: var(--surface-raised);
  border: none;
  cursor: pointer;
  color: var(--fg-muted);
}

.picker-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
}

.picker-item {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  width: 100%;
  padding: var(--space-4);
  border-radius: var(--radius-md);
  background: var(--surface-raised);
  border: 1px solid var(--border);
  font-size: 15px;
  font-weight: 600;
  color: var(--fg);
  font-family: inherit;
  cursor: pointer;
  text-align: left;
  transition: background var(--motion-default);
}
.picker-item:hover {
  background: var(--sage-50);
  border-color: var(--sage-200);
}

/* ── Entrance animation ── */
@keyframes fade-up {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
