import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { VivcordSettingsComponent } from './vivcord-settings';
import { ThemeService } from '../../../shared/services/theme.service';

describe('VivcordSettingsComponent', () => {
  let component: VivcordSettingsComponent;
  let fixture: ComponentFixture<VivcordSettingsComponent>;
  let themeService: ThemeService;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [VivcordSettingsComponent],
      providers: [provideZonelessChangeDetection(), ThemeService],
    }).compileComponents();

    fixture = TestBed.createComponent(VivcordSettingsComponent);
    component = fixture.componentInstance;
    themeService = TestBed.inject(ThemeService);
    await fixture.whenStable();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call themeService.setAccentHue when onHueChange is invoked', () => {
    component.onHueChange('180');
    expect(themeService.accentHue()).toBe(180);
  });

  it('should call themeService.setChatGradient when onChatGradientToggle is invoked', () => {
    component.onChatGradientToggle(false);
    expect(themeService.chatGradient()).toBe(false);
  });

  it('should call themeService.setFont when onFontChange is invoked', () => {
    component.onFontChange('inter');
    expect(themeService.fontId()).toBe('inter');
  });

  it('should call themeService.setTheme when onThemeSelect is invoked', () => {
    component.onThemeSelect('light');
    expect(themeService.theme()).toBe('light');
  });
});
