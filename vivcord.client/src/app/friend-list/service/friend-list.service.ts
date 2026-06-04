import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import { Friend } from '../dto/friend-list.dto';

@Injectable({
  providedIn: 'root',
})
export class FriendListService {
  private readonly apiUrl = `${environment.apiUrl}/friend`;

  httpClient = inject(HttpClient);

  getFriendList(): Observable<Friend[]> {
    return this.httpClient.get<Friend[]>(`${this.apiUrl}/list`);
  }

  addFriend(userNameToAdd: string): Observable<Friend> {
    const params = new HttpParams().set('userNameToAdd', userNameToAdd);
    return this.httpClient.post<Friend>(`${this.apiUrl}/add`, {}, { params });
  }

  removeFromFriendList(userNameToRemove: string): Observable<void> {
    const params = new HttpParams().set('userNameToRemove', userNameToRemove);
    return this.httpClient.delete<void>(`${this.apiUrl}/remove`, { params });
  }
}
