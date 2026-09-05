import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AccountService } from '@account/service/account.service';
import { LoginDTO } from '@account/dto/account-dto';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent {
  private readonly _formBuilder = inject(FormBuilder);
  private readonly _accountService = inject(AccountService);
  private readonly _router = inject(Router);

  readonly error = signal<string | null>(null);
  readonly isLoading = signal<boolean>(false);
  readonly showPassword = signal<boolean>(false);

  readonly loginForm = this._formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }
    this.isLoading.set(true);
    this.error.set(null);

    const { email, password } = this.loginForm.getRawValue();
    const dto: LoginDTO = { email, password };

    this._accountService.login(dto).subscribe({
      next: () => {
        this.isLoading.set(false);
        this._router.navigate(['/']);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.error.set(err?.error?.detail || err?.error?.title || 'Invalid email or password');
      }
    });
  }
}
