import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ThemeService, ThemeMode } from '../../../shared/services/theme.service';

@Component({
  selector: 'app-vivcord-settings',
  imports: [FormsModule],
  templateUrl: './vivcord-settings.html',
  styleUrl: './vivcord-settings.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VivcordSettingsComponent {
  public readonly themeService = inject(ThemeService);

  public onThemeSelect(mode: ThemeMode): void {
    this.themeService.setTheme(mode);
  }

  public onHueChange(hue: number | string): void {
    this.themeService.setAccentHue(Number(hue));
  }

  public onFontChange(fontId: string): void {
    this.themeService.setFont(fontId);
  }

  public onChatGradientToggle(enabled: boolean): void {
    this.themeService.setChatGradient(enabled);
  }
}
