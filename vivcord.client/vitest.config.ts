import { defineConfig } from 'vitest/config';
import path from 'path';

export default defineConfig({
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./test-setup.ts'],
    alias: {
      '@environments': path.resolve(__dirname, './src/environments'),
      '@account': path.resolve(__dirname, './src/app/account'),
    },
  },
});
