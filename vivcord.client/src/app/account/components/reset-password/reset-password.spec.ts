import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import { ResetPasswordComponent } from './reset-password';
import { AccountService } from '@account/service/account.service';

describe('ResetPasswordComponent', () => {
  let component: ResetPasswordComponent;
  let fixture: ComponentFixture<ResetPasswordComponent>;
  let accountService: AccountService;

  const createComponent = async (queryParams: Record<string, string> = { token: 'sample-token', userId: 'user-123' }) => {
    await TestBed.configureTestingModule({
      imports: [ResetPasswordComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap(queryParams),
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ResetPasswordComponent);
    component = fixture.componentInstance;
    accountService = TestBed.inject(AccountService);
    await fixture.whenStable();
  };

  describe('Valid link scenario', () => {
    beforeEach(async () => {
      await createComponent({ token: 'valid-token', userId: 'valid-user-id' });
    });

    it('should create component and extract params correctly', () => {
      expect(component).toBeTruthy();
      expect(component.isInvalidLink()).toBe(false);
      expect(component.token()).toBe('valid-token');
      expect(component.userId()).toBe('valid-user-id');
    });

    it('should validate password mismatch and requirement', () => {
      const form = component.resetForm;
      expect(form.valid).toBe(false);

      form.controls.newPassword.setValue('short');
      form.controls.confirmPassword.setValue('short');
      expect(form.controls.newPassword.hasError('minlength')).toBe(true);

      // Password without digit
      form.controls.newPassword.setValue('passwordnodigit');
      expect(form.controls.newPassword.hasError('pattern')).toBe(true);

      form.controls.newPassword.setValue('password123');
      expect(form.controls.newPassword.hasError('pattern')).toBe(false);

      form.controls.confirmPassword.setValue('different123');
      expect(form.hasError('passwordMismatch')).toBe(true);

      form.controls.confirmPassword.setValue('password123');
      expect(form.hasError('passwordMismatch')).toBe(false);
      expect(form.valid).toBe(true);
    });

    it('should calculate password strength correctly and react to value changes', () => {
      expect(component.passwordStrength().level).toBe('empty');

      component.resetForm.controls.newPassword.setValue('123');
      expect(component.passwordStrength().level).toBe('weak');

      component.resetForm.controls.newPassword.setValue('pass12');
      expect(component.passwordStrength().level).toBe('fair');

      component.resetForm.controls.newPassword.setValue('Password12');
      expect(component.passwordStrength().level).toBe('strong');

      component.resetForm.controls.newPassword.setValue('Password12!');
      expect(component.passwordStrength().level).toBe('very-strong');
    });

    it('should not submit if form is invalid', () => {
      const resetSpy = vi.spyOn(accountService, 'resetPassword');
      component.onSubmit();
      expect(resetSpy).not.toHaveBeenCalled();
      expect(component.resetForm.controls.newPassword.touched).toBe(true);
      expect(component.resetForm.controls.confirmPassword.touched).toBe(true);
    });

    it('should handle successful password reset', () => {
      component.resetForm.controls.newPassword.setValue('newSecurePassword123');
      component.resetForm.controls.confirmPassword.setValue('newSecurePassword123');

      vi.spyOn(accountService, 'resetPassword').mockReturnValue(of(undefined as unknown as void));

      component.onSubmit();

      expect(component.isLoading()).toBe(false);
      expect(component.isSuccess()).toBe(true);
      expect(component.error()).toBeNull();
    });

    it('should handle error during password reset', () => {
      component.resetForm.controls.newPassword.setValue('newSecurePassword123');
      component.resetForm.controls.confirmPassword.setValue('newSecurePassword123');

      const errorMessage = 'Token expired';
      vi.spyOn(accountService, 'resetPassword').mockReturnValue(
        throwError(() => ({ error: { detail: errorMessage } }))
      );

      component.onSubmit();

      expect(component.isLoading()).toBe(false);
      expect(component.isSuccess()).toBe(false);
      expect(component.error()).toBe(errorMessage);
    });
  });

  describe('Invalid link scenario', () => {
    it('should set isInvalidLink to true when token or userId is missing', async () => {
      TestBed.resetTestingModule();
      await createComponent({});

      expect(component.isInvalidLink()).toBe(true);
      expect(component.token()).toBe('');
      expect(component.userId()).toBe('');

      const resetSpy = vi.spyOn(accountService, 'resetPassword');
      component.onSubmit();
      expect(resetSpy).not.toHaveBeenCalled();
    });
  });
});
