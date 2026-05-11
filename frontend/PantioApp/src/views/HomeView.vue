<script setup lang="ts">
import { useRouter } from 'vue-router'
import { Archive, ShoppingCart, ChefHat, Receipt, User, ChevronRight } from 'lucide-vue-next'
import AppShell from '../components/layout/AppShell.vue'

const router = useRouter()
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
        <!-- Hero: Lager -->
        <button class="card hero" @click="router.push({ name: 'inventory-list' })">
          <div class="hero-left">
            <span class="icon-badge icon-badge--sage">
              <Archive :size="24" />
            </span>
            <div>
              <h2 class="card-title hero-title">Lager</h2>
              <p class="card-desc">Dine madvarer og udløbsdatoer</p>
            </div>
          </div>
          <ChevronRight :size="18" class="chevron" />
        </button>

        <!-- 2×2 grid -->
        <div class="grid">
          <button class="card tile tile--clay" @click="router.push('/shopping')">
            <span class="icon-badge icon-badge--clay"><ShoppingCart :size="20" /></span>
            <div class="tile-text">
              <h3 class="card-title">Indkøbsliste</h3>
              <p class="card-desc">Planlæg dine indkøb</p>
            </div>
          </button>

          <button class="card tile tile--sage" @click="router.push('/recipes')">
            <span class="icon-badge icon-badge--sage"><ChefHat :size="20" /></span>
            <div class="tile-text">
              <h3 class="card-title">Opskrifter</h3>
              <p class="card-desc">Find madopskrifter</p>
            </div>
          </button>

          <button class="card tile tile--clay" @click="router.push('/store')">
            <span class="icon-badge icon-badge--clay"><Receipt :size="20" /></span>
            <div class="tile-text">
              <h3 class="card-title">Butik</h3>
              <p class="card-desc">Tilslut Netto</p>
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
    </div>
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
  background: var(--sage-50);
  border-color: var(--sage-200);
  animation: fade-up 0.45s ease-out 0.07s both;
}

.hero:hover {
  background: var(--sage-100);
  border-color: var(--sage-300);
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
  color: var(--sage-600);
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
}

.card-desc {
  font-size: 12px;
  color: var(--fg-muted);
  line-height: 1.4;
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
