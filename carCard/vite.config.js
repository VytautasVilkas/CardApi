import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'
import tailwindcss from '@tailwindcss/vite'
import mkcert from 'vite-plugin-mkcert';
// https://vite.dev/config/
export default defineConfig({
  plugins: [react(),tailwindcss(), mkcert()],
  server: {
    headers: {
      "Cross-Origin-Opener-Policy": "same-origin-allow-popups",
      "Cross-Origin-Embedder-Policy": "unsafe-none",
    },
    https: true, 
    host: 'localhost', 
    port: 5173, 
  },
})
