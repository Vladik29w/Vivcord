import { Injectable, signal, effect, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser, DOCUMENT } from '@angular/common';

export type ThemeMode = 'dark' | 'light';

export interface FontOption {
  readonly id: string;
  readonly label: string;
  readonly cssFamily: string;
  readonly googleFontUrl: string | null;
}

export interface VivcordSettings {
  readonly theme: ThemeMode;
  readonly accentHue: number;
  readonly fontId: string;
  readonly uiScale: number;
}

const STORAGE_KEY = 'vivcord-settings';

export const DEFAULT_FONT_ID = 'red-hat-text';

export const FONT_OPTIONS: readonly FontOption[] = [
  {
    id: DEFAULT_FONT_ID,
    label: 'Red Hat Text',
    cssFamily: `'Red Hat Text', -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif`,
    googleFontUrl: 'https://fonts.googleapis.com/css2?family=Red+Hat+Text:ital,wght@0,300..700;1,300..700&display=swap',
  },
  {
    id: 'inter',
    label: 'Inter',
    cssFamily: `'Inter', -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif`,
    googleFontUrl: 'https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap',
  },
  {
    id: 'jetbrains-mono',
    label: 'JetBrains Mono',
    cssFamily: `'JetBrains Mono', ui-monospace, SFMono-Regular, Menlo, Consolas, monospace`,
    googleFontUrl: 'https://fonts.googleapis.com/css2?family=JetBrains+Mono:wght@400;500;600;700&display=swap',
  },
];

const DEFAULT_SETTINGS: VivcordSettings = {
  theme: 'dark',
  accentHue: 244,
  fontId: DEFAULT_FONT_ID,
  uiScale: 1,
};

@Injectable({
  providedIn: 'root',
})
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  public readonly theme = signal<ThemeMode>(DEFAULT_SETTINGS.theme);
  public readonly accentHue = signal<number>(DEFAULT_SETTINGS.accentHue);
  public readonly fontId = signal<string>(DEFAULT_SETTINGS.fontId);
  public readonly uiScale = signal<number>(DEFAULT_SETTINGS.uiScale);

  public readonly fonts = FONT_OPTIONS;

  private readonly loadedFonts = new Set<string>();

  constructor() {
    if (!this.isBrowser) {
      return;
    }

    this.loadFromStorage();

    effect(() => {
      this.applyToDom();
      this.persist();
    });
  }

  public setTheme(theme: ThemeMode): void {
    this.theme.set(theme);
  }

  public setAccentHue(hue: number): void {
    this.accentHue.set(Math.min(360, Math.max(0, hue)));
  }

  public setFont(fontId: string): void {
    if (!FONT_OPTIONS.some((f) => f.id === fontId)) {
      return;
    }
    const font = FONT_OPTIONS.find((f) => f.id === fontId)!;
    if (font.googleFontUrl) {
      this.loadFont(font);
    }
    this.fontId.set(fontId);
  }

  public setUiScale(scale: number): void {
    this.uiScale.set(Math.min(1.2, Math.max(0.85, scale)));
  }

  public currentFont(): FontOption {
    return FONT_OPTIONS.find((f) => f.id === this.fontId()) ?? FONT_OPTIONS[0];
  }

  private loadFromStorage(): void {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) {
        return;
      }
      const parsed = JSON.parse(raw) as Partial<VivcordSettings>;

      if (parsed.theme === 'dark' || parsed.theme === 'light') {
        this.theme.set(parsed.theme);
      }
      if (typeof parsed.accentHue === 'number' && Number.isFinite(parsed.accentHue)) {
        this.accentHue.set(Math.min(360, Math.max(0, parsed.accentHue)));
      }
      if (typeof parsed.uiScale === 'number' && Number.isFinite(parsed.uiScale)) {
        this.uiScale.set(Math.min(1.2, Math.max(0.85, parsed.uiScale)));
      }
      if (typeof parsed.fontId === 'string' && FONT_OPTIONS.some((f) => f.id === parsed.fontId)) {
        const font = FONT_OPTIONS.find((f) => f.id === parsed.fontId)!;
        if (font.googleFontUrl) {
          this.loadFont(font);
        }
        this.fontId.set(parsed.fontId);
      }
    } catch {
      // Corrupted settings — fall back to defaults
    }
  }

  private persist(): void {
    try {
      const settings: VivcordSettings = {
        theme: this.theme(),
        accentHue: this.accentHue(),
        fontId: this.fontId(),
        uiScale: this.uiScale(),
      };
      localStorage.setItem(STORAGE_KEY, JSON.stringify(settings));
    } catch {
      // Storage unavailable (private mode etc.) — settings just won't persist
    }
  }

  private applyToDom(): void {
    const root = this.document.documentElement;

    root.setAttribute('data-theme', this.theme());
    root.style.setProperty('--accent-hue', String(this.accentHue()));
    root.style.setProperty('--ui-scale', String(this.uiScale()));
    root.style.setProperty('--font-app', this.currentFont().cssFamily);
  }

  private loadFont(font: FontOption): void {
    if (!this.isBrowser || !font.googleFontUrl || this.loadedFonts.has(font.id)) {
      return;
    }
    this.loadedFonts.add(font.id);

    const link = this.document.createElement('link');
    link.rel = 'stylesheet';
    link.href = font.googleFontUrl;
    this.document.head.appendChild(link);
  }
}
