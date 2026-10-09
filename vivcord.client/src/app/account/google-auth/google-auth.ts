import {
  Component,
  inject,
  ElementRef,
  viewChild,
  output,
  signal,
  DestroyRef,
  afterNextRender,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AccountService } from '../service/account.service';

declare const google: any;

@Component({
  selector: 'app-google-auth',
  imports: [],
  templateUrl: './google-auth.html',
  styleUrl: './google-auth.css',
})
export class GoogleAuth {
  private accountService = inject(AccountService);
  private destroyRef = inject(DestroyRef);

  googleBtn = viewChild.required<ElementRef>('googleBtn');

  readonly success = output<void>();
  readonly error = output<string>();
  readonly loading = signal(false);

  private readonly clientId =
    '321360609639-l56466ohdeu1umddm9vstbfeai331jjh.apps.googleusercontent.com';

  constructor() {
    afterNextRender(() => {
      if (typeof google === 'undefined') return;

      google.accounts.id.initialize({
        client_id: this.clientId,
        callback: (response: { credential: string }) =>
          this.handleCredential(response.credential),
        ux_mode: 'popup',
      });

      google.accounts.id.renderButton(this.googleBtn().nativeElement, {
        type: 'standard',
        theme: 'filled_black',
        size: 'large',
        shape: 'rectangular',
        logo_alignment: 'left',
        width: 280,
      });
    });
  }

  private handleCredential(idToken: string): void {
    this.loading.set(true);

    this.accountService
      .googleLogin(idToken)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.loading.set(false);
          this.success.emit();
        },
        error: (err) => {
          this.loading.set(false);
          this.error.emit(err?.error?.message ?? 'Google login failed');
        },
      });
  }
}
