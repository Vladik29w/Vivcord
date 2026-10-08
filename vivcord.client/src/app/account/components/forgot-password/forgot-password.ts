import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AccountService } from '@account/service/account.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ForgotPasswordComponent {
  private readonly _formBuilder = inject(FormBuilder);
  readonly accountService = inject(AccountService);

  readonly isLoading = signal<boolean>(false);
  readonly error = signal<string | null>(null);
  readonly isSubmitted = signal<boolean>(false);
  readonly submittedEmail = signal<string>('');

  readonly forgotForm = this._formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  onSubmit(): void {
    if (this.forgotForm.invalid) {
      this.forgotForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.error.set(null);

    const { email } = this.forgotForm.getRawValue();

    this.accountService.forgotPassword(email).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.submittedEmail.set(email);
        this.isSubmitted.set(true);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.error.set(
          err?.error?.detail ||
          err?.error?.title ||
          'Failed to send password reset email. Please try again.'
        );
      },
    });
  }

  resetForm(): void {
    this.isSubmitted.set(false);
    this.error.set(null);
    this.forgotForm.reset();
  }
}

export { ForgotPasswordComponent as ForgotPassword };
