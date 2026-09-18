import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  let service: ThemeService;

  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.style.removeProperty('--accent-hue');
    document.documentElement.style.removeProperty('--font-app');
    document.documentElement.style.removeProperty('--chat-bg');

    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), ThemeService],
    });

    service = TestBed.inject(ThemeService);
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should be created with default values', () => {
    expect(service).toBeTruthy();
    expect(service.theme()).toBe('dark');
    expect(service.accentHue()).toBe(244);
    expect(service.fontId()).toBe('red-hat-text');
    expect(service.chatGradient()).toBe(true);
  });

  it('should update theme and apply data-theme to documentElement', () => {
    service.setTheme('light');
    TestBed.tick();

    expect(service.theme()).toBe('light');
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
  });

  it('should update accentHue and clamp within range [0, 360]', () => {
    service.setAccentHue(120);
    TestBed.tick();
    expect(service.accentHue()).toBe(120);
    expect(document.documentElement.style.getPropertyValue('--accent-hue')).toBe('120');

    service.setAccentHue(400);
    TestBed.tick();
    expect(service.accentHue()).toBe(360);

    service.setAccentHue(-10);
    TestBed.tick();
    expect(service.accentHue()).toBe(0);
  });

  it('should update fontId and apply font to documentElement', () => {
    service.setFont('inter');
    TestBed.tick();

    expect(service.fontId()).toBe('inter');
    expect(document.documentElement.style.getPropertyValue('--font-app')).toContain('Inter');
  });

  it('should toggle chatGradient and update --chat-bg', () => {
    service.setChatGradient(false);
    TestBed.tick();

    expect(service.chatGradient()).toBe(false);
    expect(document.documentElement.style.getPropertyValue('--chat-bg')).toBe('#121212');
  });

  it('should persist settings to localStorage', () => {
    service.setAccentHue(180);
    service.setFont('jetbrains-mono');
    TestBed.tick();

    const stored = JSON.parse(localStorage.getItem('vivcord-settings') || '{}');
    expect(stored.accentHue).toBe(180);
    expect(stored.fontId).toBe('jetbrains-mono');
    expect(stored.uiScale).toBeUndefined();
  });
});
