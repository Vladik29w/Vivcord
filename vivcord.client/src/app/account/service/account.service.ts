import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, of } from 'rxjs';
import { UserDTO, RegisterDTO, LoginDTO } from '@dto/account-dto';
import { environment } from '@environments/environment';

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  private url = `${environment.apiUrl}/account`;
  private http = inject(HttpClient);
  currentUser = signal<UserDTO | null>(null);

  private authenticate<T>(endpoint: 'login' | 'register' | 'refresh', data: T): Observable<UserDTO> {
    return this.http.post<UserDTO>(`${this.url}/${endpoint}`, data).pipe(
      tap(user => this.currentUser.set(user))
    );
  }
  login(data: LoginDTO): Observable<UserDTO> {
    return this.authenticate('login', data);
  }
  register(data: RegisterDTO): Observable<UserDTO> {
    return this.authenticate('register', data);
  }
  logout(): void {
    this.http.post(`${this.url}/logout`, {}).subscribe({
      next: () => {
        this.currentUser.set(null);
      },
      error: (err) => {
        this.currentUser.set(null);
        console.error(err);
      }
    });
  }
  refresh(): Observable<UserDTO> {
    return this.authenticate('refresh', {})
  }
  checkUser(): Observable<UserDTO | null> {
    return this.http.get<UserDTO>(`${this.url}/me`).pipe(
      tap({
        next: (user) => this.currentUser.set(user),
        error: () => this.currentUser.set(null)
      }),
      catchError((err) => {
        return of(null);
      })
    );
  }
}
