import { Component, inject, signal, computed, ChangeDetectionStrategy, DestroyRef } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AccountService } from '@account/service/account.service';
import { RegisterDTO } from '@account/dto/account.dto';
import { GoogleAuth } from '@account/google-auth/google-auth';

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
  imports: [ReactiveFormsModule, RouterLink, GoogleAuth],
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

    this.registerForm.controls.name.valueChanges
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe(() => {
        if (this.registerForm.controls.name.hasError('taken')) {
          const errors = { ...this.registerForm.controls.name.errors };
          delete errors['taken'];
          this.registerForm.controls.name.setErrors(Object.keys(errors).length ? errors : null);
        }
        if (this.error()) {
          this.error.set(null);
        }
      });

    this.registerForm.controls.email.valueChanges
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe(() => {
        if (this.registerForm.controls.email.hasError('taken')) {
          const errors = { ...this.registerForm.controls.email.errors };
          delete errors['taken'];
          this.registerForm.controls.email.setErrors(Object.keys(errors).length ? errors : null);
        }
        if (this.error()) {
          this.error.set(null);
        }
      });
  }

  getFieldError(field: 'name' | 'email'): string | null {
    const control = this.registerForm.get(field);
    const takenErr = control?.getError('taken');
    if (typeof takenErr === 'string') {
      return takenErr;
    }
    if (takenErr) {
      return field === 'name' ? 'This username is already taken' : 'This email is already registered';
    }
    return null;
  }

  onGoogleSuccess(): void {
    this.error.set(null);
    this._router.navigate(['/']);
  }

  onGoogleError(errorMessage: string): void {
    this.error.set(errorMessage || 'Google sign-up failed');
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
        this.handleRegistrationError(err);
      }
    });
  }

  private handleRegistrationError(err: unknown): void {
    this.isLoading.set(false);

    if (!(err instanceof HttpErrorResponse)) {
      this.error.set('Registration failed. Please try again.');
      return;
    }

    const errorData = err.error;
    let usernameError: string | null = null;
    let emailError: string | null = null;
    let generalError: string | null = null;

    // 1. Check if errorData has a validation errors dictionary (e.g. ASP.NET ValidationProblem)
    if (errorData?.errors && typeof errorData.errors === 'object') {
      for (const [key, messages] of Object.entries(errorData.errors)) {
        const lowerKey = key.toLowerCase();
        const msgList = Array.isArray(messages) ? messages : [String(messages)];
        const combinedMsg = msgList.join(' ');

        if (
          lowerKey.includes('username') ||
          lowerKey.includes('name') ||
          combinedMsg.toLowerCase().includes('user name') ||
          combinedMsg.toLowerCase().includes('username') ||
          combinedMsg.toLowerCase().includes('nickname')
        ) {
          usernameError = combinedMsg.toLowerCase().includes('taken')
            ? 'This username is already taken'
            : combinedMsg || 'This username is already taken';
        } else if (
          lowerKey.includes('email') ||
          combinedMsg.toLowerCase().includes('email')
        ) {
          emailError = combinedMsg.toLowerCase().includes('taken') || combinedMsg.toLowerCase().includes('registered')
            ? 'This email is already registered'
            : combinedMsg || 'This email is already registered';
        } else if (!generalError) {
          generalError = combinedMsg;
        }
      }
    }

    // 2. Check raw error strings or message properties (409 Conflict ProblemDetails, code, title, detail)
    const rawMessage = (
      (typeof errorData === 'string' ? errorData : '') ||
      errorData?.detail ||
      errorData?.title ||
      errorData?.message ||
      errorData?.code ||
      ''
    ).toString();

    const lowerMsg = rawMessage.toLowerCase();

    if (
      lowerMsg.includes('username') ||
      lowerMsg.includes('user name') ||
      lowerMsg.includes('nickname') ||
      lowerMsg.includes('duplicateusername') ||
      lowerMsg.includes('usernamealreadytaken')
    ) {
      usernameError = usernameError || 'This username is already taken';
    }

    if (
      lowerMsg.includes('email') ||
      lowerMsg.includes('duplicateemail') ||
      lowerMsg.includes('emailalreadyregistered')
    ) {
      emailError = emailError || 'This email is already registered';
    }

    // 3. Fallback for 409 conflict when neither username nor email was specifically isolated
    if (err.status === 409 && !usernameError && !emailError) {
      emailError = 'This email is already registered';
    }

    if (usernameError) {
      this.registerForm.controls.name.setErrors({ taken: usernameError });
      this.registerForm.controls.name.markAsTouched();
    }

    if (emailError) {
      this.registerForm.controls.email.setErrors({ taken: emailError });
      this.registerForm.controls.email.markAsTouched();
    }

    if (usernameError && emailError) {
      this.error.set('Username and email are already in use.');
    } else if (usernameError) {
      this.error.set(usernameError);
    } else if (emailError) {
      this.error.set(emailError);
    } else {
      this.error.set(generalError || rawMessage || 'Registration failed. Please try again.');
    }
  }
}
