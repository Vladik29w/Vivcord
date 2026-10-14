import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, Router } from '@angular/router';
import { provideHttpClient, HttpErrorResponse } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { throwError } from 'rxjs';
import { AccountService } from '@account/service/account.service';
import { RegisterComponent } from './register';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should navigate to home on google auth success', () => {
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');

    component.error.set('Previous error');
    component.onGoogleSuccess();

    expect(component.error()).toBeNull();
    expect(navigateSpy).toHaveBeenCalledWith(['/']);
  });

  it('should set error on google auth failure', () => {
    component.onGoogleError('Google registration failed');
    expect(component.error()).toBe('Google registration failed');
  });

  it('should handle duplicate username conflict (409)', () => {
    const accountService = TestBed.inject(AccountService);
    vi.spyOn(accountService, 'register').mockReturnValue(
      throwError(() => new HttpErrorResponse({
        status: 409,
        error: { title: 'Username is already taken' },
      }))
    );

    component.registerForm.setValue({
      name: 'taken_user',
      email: 'new@example.com',
      password: 'Password1',
      confirmPassword: 'Password1',
    });

    component.onSubmit();

    expect(component.registerForm.controls.name.hasError('taken')).toBe(true);
    expect(component.getFieldError('name')).toBe('This username is already taken');
    expect(component.registerForm.controls.email.hasError('taken')).toBe(false);
    expect(component.error()).toBe('This username is already taken');
  });

  it('should handle duplicate username from validation errors (400)', () => {
    const accountService = TestBed.inject(AccountService);
    vi.spyOn(accountService, 'register').mockReturnValue(
      throwError(() => new HttpErrorResponse({
        status: 400,
        error: {
          title: 'One or more validation errors occurred.',
          errors: {
            DuplicateUserName: ["User name 'taken_user' is already taken."],
          },
        },
      }))
    );

    component.registerForm.setValue({
      name: 'taken_user',
      email: 'new@example.com',
      password: 'Password1',
      confirmPassword: 'Password1',
    });

    component.onSubmit();

    expect(component.registerForm.controls.name.hasError('taken')).toBe(true);
    expect(component.getFieldError('name')).toBe('This username is already taken');
    expect(component.registerForm.controls.email.hasError('taken')).toBe(false);
  });

  it('should handle duplicate email conflict (409)', () => {
    const accountService = TestBed.inject(AccountService);
    vi.spyOn(accountService, 'register').mockReturnValue(
      throwError(() => new HttpErrorResponse({
        status: 409,
        error: { title: 'Email is already registered' },
      }))
    );

    component.registerForm.setValue({
      name: 'valid_user',
      email: 'taken@example.com',
      password: 'Password1',
      confirmPassword: 'Password1',
    });

    component.onSubmit();

    expect(component.registerForm.controls.email.hasError('taken')).toBe(true);
    expect(component.getFieldError('email')).toBe('This email is already registered');
    expect(component.registerForm.controls.name.hasError('taken')).toBe(false);
    expect(component.error()).toBe('This email is already registered');
  });

  it('should clear taken error and error banner when user changes input', () => {
    component.registerForm.controls.name.setErrors({ taken: 'This username is already taken' });
    component.error.set('This username is already taken');

    component.registerForm.controls.name.setValue('new_unique_user');

    expect(component.registerForm.controls.name.hasError('taken')).toBe(false);
    expect(component.error()).toBeNull();
  });
});

