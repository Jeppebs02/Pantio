import { createApp } from 'vue'
import { createPinia } from 'pinia'
import router from './router'
import './assets/styles/base.css'
import App from './App.vue'
import { useAuthStore } from './stores/auth'

const pinia = createPinia()
const app = createApp(App).use(pinia)

// Start auth initialization before the router installs so the guard
// always has accurate state when it first runs.
useAuthStore().initialize()

app.use(router).mount('#app')
