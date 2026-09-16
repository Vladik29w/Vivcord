import { ApplicationConfig, provideAppInitializer, inject, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { authInterceptor } from './auth.interceptor'
import { AccountService } from '../app/account/service/account.service';
import { ThemeService } from './shared/services/theme.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(
      withInterceptors([authInterceptor]),
    ),
    provideAppInitializer(() => {
      // Eagerly instantiate ThemeService so saved settings are applied before first render
      inject(ThemeService);
      const accountService = inject(AccountService)
      return accountService.checkUser();
    })
  ]
};
