import { inject } from '@angular/core';
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { AccountService } from '../app/account/service/account.service';
import { switchMap, catchError, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const accountService = inject(AccountService);
  const authReq = req.clone({
    withCredentials: true
  });
  return next(authReq).pipe(
    catchError((err) => {
      if (err instanceof HttpErrorResponse && err.status === 401 && !req.url.includes('login')) {
        return accountService.refresh().pipe(
          switchMap(() => {
            return next(authReq);
          }), 
        )
      }
      return throwError(() => err);
    }) 
  )
}
