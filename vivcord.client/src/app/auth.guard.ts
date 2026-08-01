import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AccountService } from './account/service/account.service';

export const authGuard: CanActivateFn = () => {
  const accountService = inject(AccountService);
  const router = inject(Router);

  if (accountService.currentUser()) {
    return true;
  }

  return router.createUrlTree(['/login']);
};

export const guestGuard: CanActivateFn = () => {
  const accountService = inject(AccountService);
  const router = inject(Router);

  if (!accountService.currentUser()) {
    return true;
  }

  return router.createUrlTree(['/']);
};
