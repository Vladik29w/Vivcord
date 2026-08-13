import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AccountService } from '@account/service/account.service';
import { LoginDTO } from '@account/dto/account-dto';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent {
  //injects
  private _formBuilder = inject(FormBuilder);
  private _accountService = inject(AccountService);
  private _router = inject(Router);
  //signals
  error = signal<string | null>(null);
  isLoading = signal<boolean>(false);
  //form
  loginForm = this._formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched()
      return;
    }
    this.isLoading.set(true);
    this.error.set(null);

    const { email, password } = this.loginForm.getRawValue();
    const dto: LoginDTO = { email, password };

    this._accountService.login(dto).subscribe({
      next: () => {
        this.isLoading.set(false)
        this._router.navigate(['/'])
      },
      error: (err) => {
        this.isLoading.set(false);
        this.error.set('Login failed');
      }
    })
  }
}
