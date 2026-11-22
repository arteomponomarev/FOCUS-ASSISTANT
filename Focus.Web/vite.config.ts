import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), 'FOCUS_')
  const proxy = {
    '/api': {
      target: env.FOCUS_API_ORIGIN || 'http://127.0.0.1:8080',
      changeOrigin: true,
    },
  }

  return {
    plugins: [react()],
    server: { host: '127.0.0.1', proxy },
    preview: { host: '127.0.0.1', proxy },
  }
})
