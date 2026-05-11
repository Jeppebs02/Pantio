<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { User, LogOut, Trash2, Receipt } from 'lucide-vue-next'
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
import PButton from '../../components/ui/PButton.vue'
import PBadge from '../../components/ui/PBadge.vue'
import { useAuthStore } from '../../stores/auth'
import { useStoreConnectionStore } from '../../stores/storeConnection'
import { deleteUser } from '../../services/users'

const auth = useAuthStore()
const storeConn = useStoreConnectionStore()
const isDeleting = ref(false)

onMounted(async () => {
  if (auth.localUser) {
    await storeConn.fetchConnections()
  }
})

async function handleDeleteAccount() {
  if (!confirm('Delete your account? This cannot be undone.')) return
  if (!confirm('Are you sure? All your inventory data will be lost.')) return
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
      <TopBar title="You" />
    </template>

    <div class="page">
      <!-- User card -->
      <div class="card">
        <div class="user-header">
          <div class="user-avatar">
            <User :size="28" />
          </div>
          <div>
            <p class="eyebrow">Signed in as</p>
            <h3>{{ auth.auth0User?.email ?? auth.localUser?.email ?? 'Unknown' }}</h3>
          </div>
        </div>
      </div>

      <!-- Store connections -->
      <div class="card">
        <h3>Store connections</h3>
        <div class="connection-row">
          <div class="connection-info">
            <Receipt :size="18" />
            <span>Netto</span>
          </div>
          <PBadge
            :tone="storeConn.nettoStatus === 'active' ? 'fresh' : 'neutral'"
            :dot="true"
          >
            {{ storeConn.nettoStatus === 'active' ? 'Connected' : 'Not connected' }}
          </PBadge>
        </div>
      </div>

      <!-- Actions -->
      <div class="card actions-card">
        <PButton variant="secondary" full-width @click="auth.logout()">
          <LogOut :size="16" />
          Sign out
        </PButton>
      </div>

      <!-- Danger zone -->
      <div class="card danger-card">
        <h3 class="danger-title">Danger zone</h3>
        <p class="danger-desc">Deleting your account removes all your data permanently.</p>
        <PButton variant="danger" :disabled="isDeleting" @click="handleDeleteAccount">
          <Trash2 :size="16" />
          {{ isDeleting ? 'Deleting...' : 'Delete account' }}
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
