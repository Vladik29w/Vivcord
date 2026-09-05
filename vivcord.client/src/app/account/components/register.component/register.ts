import { Component, inject, signal, computed, ChangeDetectionStrategy, DestroyRef } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AccountService } from '@account/service/account.service';
import { RegisterDTO } from '@account/dto/account-dto';

export type PasswordStrengthLevel = 'empty' | 'weak' | 'fair' | 'strong' | 'very-strong';

export interface PasswordStrength {
  level: PasswordStrengthLevel;
  score: number;
  label: string;
  color: string;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterComponent {
  private readonly _formBuilder = inject(FormBuilder);
  private readonly _accountService = inject(AccountService);
  private readonly _router = inject(Router);
  private readonly _destroyRef = inject(DestroyRef);

  readonly error = signal<string | null>(null);
  readonly isLoading = signal<boolean>(false);
  readonly showPassword = signal<boolean>(false);
  readonly showConfirmPassword = signal<boolean>(false);
  readonly passwordValue = signal<string>('');

  readonly registerForm = this._formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6), Validators.pattern(/(?=.*\d)/)]],
    confirmPassword: ['', [Validators.required]]
  }, {
    validators: [this.passwordMatch]
  });

  readonly passwordStrength = computed<PasswordStrength>(() => {
    return this.calculatePasswordStrength(this.passwordValue());
  });

  constructor() {
    this.registerForm.controls.password.valueChanges
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe((val) => {
        this.passwordValue.set(val || '');
      });
  }

  public calculatePasswordStrength(password: string): PasswordStrength {
    if (!password || password.length === 0) {
      return { level: 'empty', score: 0, label: '', color: '' };
    }

    const hasMin6 = password.length >= 6;
    const hasMin8 = password.length >= 8;
    const hasDigit = /\d/.test(password);
    const hasUpper = /[A-Z]/.test(password);
    const hasSpecial = /[^A-Za-z0-9]/.test(password);

    // very strong: 8+ символів і має І цифру І велику літеру І спец символ
    if (hasMin8 && hasDigit && hasUpper && hasSpecial) {
      return {
        level: 'very-strong',
        score: 4,
        label: 'Very strong',
        color: '#15803d', // Dark green / emerald
      };
    }

    // strong: 8+ символів, має цифру і має АБО велику літеру АБО спец символ
    if (hasMin8 && hasDigit && (hasUpper || hasSpecial)) {
      return {
        level: 'strong',
        score: 3,
        label: 'Strong',
        color: '#84cc16', // Light green / lime
      };
    }

    // fair: 6+ символів і має цифру
    if (hasMin6 && hasDigit) {
      return {
        level: 'fair',
        score: 2,
        label: 'Fair',
        color: '#f97316', // Orange
      };
    }

    // weak: не підходить по правилам (менше 6 символів або нема цифри)
    return {
      level: 'weak',
      score: 1,
      label: 'Weak',
      color: '#ef4444', // Red
    };
  }

  private passwordMatch(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }
    this.isLoading.set(true);
    this.error.set(null);

    const { name, email, password } = this.registerForm.getRawValue();
    const dto: RegisterDTO = { name, email, password };

    this._accountService.register(dto).subscribe({
      next: () => {
        this.isLoading.set(false);
        this._router.navigate(['/']);
      },
      error: (err) => {
        this.isLoading.set(false);
        if (err instanceof HttpErrorResponse && err.status === 409) {
          this.error.set('Email is already registered');
        } else {
          this.error.set(err?.error?.detail || err?.error?.title || 'Registration failed');
        }
      }
    });
  }
}
