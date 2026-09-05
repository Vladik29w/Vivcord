import { defineConfig, Plugin } from 'vitest/config';
import path from 'path';
import fs from 'fs';

function angularInlinePlugin(): Plugin {
  return {
    name: 'angular-inline-plugin',
    transform(code, id) {
      if (!id.endsWith('.ts') || id.includes('node_modules')) return;

      let transformed = code;
      transformed = transformed.replace(/templateUrl:\s*['"]([^'"]+)['"]/g, (_, templateUrl) => {
        const filePath = path.resolve(path.dirname(id), templateUrl);
        if (fs.existsSync(filePath)) {
          const content = fs.readFileSync(filePath, 'utf-8');
          return `template: ${JSON.stringify(content)}`;
        }
        return _;
      });

      transformed = transformed.replace(/styleUrl:\s*['"]([^'"]+)['"]/g, (_, styleUrl) => {
        const filePath = path.resolve(path.dirname(id), styleUrl);
        if (fs.existsSync(filePath)) {
          const content = fs.readFileSync(filePath, 'utf-8');
          return `styles: [${JSON.stringify(content)}]`;
        }
        return _;
      });

      return { code: transformed };
    }
  };
}

export default defineConfig({
  plugins: [angularInlinePlugin()],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./test-setup.ts'],
    server: {
      deps: {
        inline: ['livekit-rnnoise-processor', 'livekit-client'],
      },
    },
    alias: {
      '@environments': path.resolve(__dirname, './src/environments'),
      '@account': path.resolve(__dirname, './src/app/account'),
    },
  },
});

