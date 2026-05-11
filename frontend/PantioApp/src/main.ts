import { createApp } from 'vue'
import { createPinia } from 'pinia'
import router from './router'
import './assets/styles/base.css'
import App from './App.vue'

createApp(App).use(createPinia()).use(router).mount('#app')
