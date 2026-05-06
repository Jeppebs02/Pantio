import path from 'node:path'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig(({ command }) => ({
  plugins: [vue()],
  envDir: path.resolve(__dirname, '..', '..'),
  mode: command === 'build' ? 'prod' : 'dev',
}))
