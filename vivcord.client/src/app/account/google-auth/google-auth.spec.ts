import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { GoogleAuth } from './google-auth';
import { AccountService } from '../service/account.service';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';

describe('GoogleAuth', () => {
  let component: GoogleAuth;
  let fixture: ComponentFixture<GoogleAuth>;
  let accountService: AccountService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GoogleAuth],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(GoogleAuth);
    component = fixture.componentInstance;
    accountService = TestBed.inject(AccountService);
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should emit success when googleLogin succeeds', () => {
    const googleLoginSpy = vi.spyOn(accountService, 'googleLogin').mockReturnValue(
      of({
        token: 'fake-token',
        expiresAt: '2026-01-01',
        user: { id: '1', email: 'test@example.com', displayName: 'Test', roles: [] },
      })
    );

    let successEmitted = false;
    component.success.subscribe(() => {
      successEmitted = true;
    });

    (component as any).handleCredential('valid-id-token');

    expect(googleLoginSpy).toHaveBeenCalledWith('valid-id-token');
    expect(successEmitted).toBe(true);
    expect(component.loading()).toBe(false);
  });

  it('should emit error when googleLogin fails', () => {
    vi.spyOn(accountService, 'googleLogin').mockReturnValue(
      throwError(() => ({
        error: { message: 'Google token is invalid' },
      }))
    );

    let emittedError = '';
    component.error.subscribe((msg) => {
      emittedError = msg;
    });

    (component as any).handleCredential('invalid-id-token');

    expect(emittedError).toBe('Google token is invalid');
    expect(component.loading()).toBe(false);
  });
});
