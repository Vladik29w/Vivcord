import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { AccountService } from '@account/service/account.service';
import { RegisterDTO } from '@account/dto/account-dto';
@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class RegisterComponent {
  //injects
  private _formBuilder = inject(FormBuilder);
  private _accountService = inject(AccountService);
  private _router = inject(Router);
  //signals
  error = signal<string | null>(null);
  isLoading = signal<boolean>(false);

  registerForm = this._formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6), Validators.pattern(/(?=.*\d)/)]],
    confirmPassword: ['', [Validators.required]]
  }, {
    validators: [this.passwordMatch]
  });

  private passwordMatch(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched()
      return;
    }
    this.isLoading.set(true);
    this.error.set(null);

    const { name, email, password } = this.registerForm.getRawValue();
    const dto: RegisterDTO = { name, email, password };

    this._accountService.register(dto).subscribe({
      next: () => {
        this.isLoading.set(false)
        this._router.navigate(['/'])
      },
      error: (err) => {
        this.isLoading.set(false);
        if (err instanceof HttpErrorResponse && err.status === 409) {
          this.error.set('Email is already registered');
        } else {
          this.error.set('Registration failed');
        }
      }
    })
  }
}
