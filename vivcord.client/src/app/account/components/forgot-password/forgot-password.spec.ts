import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { ForgotPasswordComponent } from './forgot-password';
import { AccountService } from '@account/service/account.service';

describe('ForgotPasswordComponent', () => {
  let component: ForgotPasswordComponent;
  let fixture: ComponentFixture<ForgotPasswordComponent>;
  let accountService: AccountService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ForgotPasswordComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ForgotPasswordComponent);
    component = fixture.componentInstance;
    accountService = TestBed.inject(AccountService);
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with invalid empty form and default state', () => {
    expect(component.forgotForm.valid).toBe(false);
    expect(component.isLoading()).toBe(false);
    expect(component.isSubmitted()).toBe(false);
    expect(component.error()).toBeNull();
    expect(component.submittedEmail()).toBe('');
  });

  it('should validate email format', () => {
    const emailControl = component.forgotForm.controls.email;
    emailControl.setValue('invalid-email');
    expect(emailControl.valid).toBe(false);
    expect(emailControl.hasError('email')).toBe(true);

    emailControl.setValue('user@example.com');
    expect(emailControl.valid).toBe(true);
  });

  it('should not call accountService when form is invalid', () => {
    const spy = vi.spyOn(accountService, 'forgotPassword');
    component.onSubmit();
    expect(spy).not.toHaveBeenCalled();
    expect(component.forgotForm.controls.email.touched).toBe(true);
  });

  it('should handle successful password reset request', () => {
    const email = 'user@example.com';
    component.forgotForm.controls.email.setValue(email);

    vi.spyOn(accountService, 'forgotPassword').mockReturnValue(of(undefined as unknown as void));

    component.onSubmit();

    expect(component.isLoading()).toBe(false);
    expect(component.isSubmitted()).toBe(true);
    expect(component.submittedEmail()).toBe(email);
    expect(component.error()).toBeNull();
  });

  it('should handle error when password reset request fails', () => {
    const email = 'user@example.com';
    component.forgotForm.controls.email.setValue(email);

    const errorMessage = 'User does not exist';
    vi.spyOn(accountService, 'forgotPassword').mockReturnValue(
      throwError(() => ({ error: { detail: errorMessage } }))
    );

    component.onSubmit();

    expect(component.isLoading()).toBe(false);
    expect(component.isSubmitted()).toBe(false);
    expect(component.error()).toBe(errorMessage);
  });

  it('should reset form state on resetForm()', () => {
    component.isSubmitted.set(true);
    component.error.set('some error');
    component.forgotForm.controls.email.setValue('user@example.com');

    component.resetForm();

    expect(component.isSubmitted()).toBe(false);
    expect(component.error()).toBeNull();
    expect(component.forgotForm.controls.email.value).toBe('');
  });
});
