import { inject } from '@angular/core';
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { AccountService } from '../app/account/service/account.service';
import { switchMap, catchError, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const accountService = inject(AccountService);

  const authReq = req.clone({
    withCredentials: true
  });

  const isAuthEndpoint = req.url.includes('/account/login') ||
                         req.url.includes('/account/register') ||
                         req.url.includes('/account/refresh');

  return next(authReq).pipe(
    catchError((err) => {
      if (err instanceof HttpErrorResponse && err.status === 401 && !isAuthEndpoint) {
        return accountService.refresh().pipe(
          switchMap(() => {
            return next(authReq);
          }),
          catchError((refreshErr) => {
            accountService.currentUser.set(null);
            return throwError(() => refreshErr);
          })
        );
      }
      return throwError(() => err);
    })
  );
};
