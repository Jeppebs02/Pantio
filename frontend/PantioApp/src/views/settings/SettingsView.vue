<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { User, LogOut, Trash2, Receipt, ShieldCheck } from 'lucide-vue-next'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import PButton from '../../components/ui/PButton.vue'
import PBadge from '../../components/ui/PBadge.vue'
import PAlert from '../../components/ui/PAlert.vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../../stores/auth'
import { useStoreConnectionStore } from '../../stores/storeConnection'
import { deleteUser } from '../../services/users'

const router = useRouter()
const auth = useAuthStore()
const storeConn = useStoreConnectionStore()
const isDeleting = ref(false)

const daysUntilDeletion = computed(() => {
  const warned = auth.localUser?.deletionWarningSentAt
  if (!warned) return null
  const deleteAt = new Date(warned)
  deleteAt.setDate(deleteAt.getDate() + 30)
  const days = Math.ceil((deleteAt.getTime() - Date.now()) / 86_400_000)
  return Math.max(days, 1)
})

onMounted(async () => {
  if (auth.localUser) {
    await storeConn.fetchConnections()
  }
})

async function handleDeleteAccount() {
  if (!confirm('Slet din konto? Dette kan ikke fortrydes.')) return
  if (!confirm('Er du sikker? Alle dine lagerdata vil gå tabt.')) return
  isDeleting.value = true
  try {
    await deleteUser(auth.localUser!.id)
    await auth.logout()
  } finally {
    isDeleting.value = false
  }
}
</script>

<template>
  <AppShell>
    <template #topbar>
      <TopBar title="Dig" />
    </template>

    <div class="page">
      <!-- Deletion warning -->
      <PAlert v-if="daysUntilDeletion !== null" variant="warning" title="Konto planlagt til sletning">
        Din konto har været inaktiv og vil blive slettet om
        <strong>{{ daysUntilDeletion }} {{ daysUntilDeletion === 1 ? 'dag' : 'dage' }}</strong>.
        Brug blot appen for at annullere sletningen.
      </PAlert>

      <!-- User card -->
      <div class="card">
        <div class="user-header">
          <div class="user-avatar">
            <User :size="28" />
          </div>
          <div>
            <p class="eyebrow">Logget ind som</p>
            <h3>{{ auth.auth0User?.email ?? auth.localUser?.email ?? 'Unknown' }}</h3>
          </div>
        </div>
      </div>

      <!-- Store connections -->
      <div class="card">
        <h3>Butiksintegrationer</h3>
        <div class="connection-row">
          <div class="connection-info">
            <Receipt :size="18" />
            <span>Netto</span>
          </div>
          <PBadge
            :tone="storeConn.nettoStatus === 'active' ? 'fresh' : 'neutral'"
            :dot="true"
          >
            {{ storeConn.nettoStatus === 'active' ? 'Tilsluttet' : 'Ikke tilsluttet' }}
          </PBadge>
        </div>
      </div>

      <!-- Actions -->
      <div class="card actions-card">
        <PButton variant="secondary" full-width @click="router.push({ name: 'privacy' })">
          <ShieldCheck :size="16" />
          Privatliv & dataopbevaring
        </PButton>
        <PButton variant="secondary" full-width @click="auth.logout()">
          <LogOut :size="16" />
          Log ud
        </PButton>
      </div>

      <!-- Danger zone -->
      <div class="card danger-card">
        <h3 class="danger-title">Farezonen</h3>
        <p class="danger-desc">Sletning af din konto fjerner alle dine data permanent.</p>
        <PButton variant="danger" :disabled="isDeleting" @click="handleDeleteAccount">
          <Trash2 :size="16" />
          {{ isDeleting ? 'Sletter...' : 'Slet konto' }}
        </PButton>
      </div>
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

.user-header {
  display: flex;
  align-items: center;
  gap: var(--space-3);
}

.user-avatar {
  width: 52px;
  height: 52px;
  border-radius: 50%;
  background: var(--sage-100);
  color: var(--sage-600);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.connection-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.connection-info {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  font-size: 15px;
  color: var(--fg);
}

.danger-title {
  color: var(--past);
}

.danger-desc {
  font-size: 14px;
  color: var(--fg-muted);
  margin-top: calc(-1 * var(--space-2));
}
</style>
