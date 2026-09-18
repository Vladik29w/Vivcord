import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  let service: ThemeService;

  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.style.removeProperty('--accent-hue');
    document.documentElement.style.removeProperty('--ui-scale');
    document.documentElement.style.removeProperty('zoom');
    document.documentElement.style.removeProperty('--font-app');

    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), ThemeService],
    });

    service = TestBed.inject(ThemeService);
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should be created with default uiScale of 1', () => {
    expect(service).toBeTruthy();
    expect(service.uiScale()).toBe(1);
  });

  it('should update uiScale and apply zoom and --ui-scale to documentElement', async () => {
    service.setUiScale(1.15);
    TestBed.tick();

    expect(service.uiScale()).toBe(1.15);
    expect(document.documentElement.style.getPropertyValue('--ui-scale')).toBe('1.15');
    expect(document.documentElement.style.getPropertyValue('zoom')).toBe('1.15');
  });

  it('should clamp uiScale within range [0.85, 1.2]', () => {
    service.setUiScale(2.0);
    TestBed.tick();
    expect(service.uiScale()).toBe(1.2);
    expect(document.documentElement.style.getPropertyValue('zoom')).toBe('1.2');

    service.setUiScale(0.5);
    TestBed.tick();
    expect(service.uiScale()).toBe(0.85);
    expect(document.documentElement.style.getPropertyValue('zoom')).toBe('0.85');
  });

  it('should preserve font selection when scaling and vice-versa', () => {
    service.setFont('inter');
    service.setUiScale(1.1);
    TestBed.tick();

    expect(service.fontId()).toBe('inter');
    expect(service.uiScale()).toBe(1.1);
    expect(document.documentElement.style.getPropertyValue('zoom')).toBe('1.1');
    expect(document.documentElement.style.getPropertyValue('--font-app')).toContain('Inter');
  });

  it('should persist uiScale to localStorage', () => {
    service.setUiScale(1.1);
    TestBed.tick();

    const stored = JSON.parse(localStorage.getItem('vivcord-settings') || '{}');
    expect(stored.uiScale).toBe(1.1);
  });
});
