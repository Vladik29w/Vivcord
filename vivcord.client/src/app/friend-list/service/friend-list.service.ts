import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
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

  addFriend(username: string): Observable<Friend> {
    return this.httpClient.post<Friend>(`${this.apiUrl}/add`, { username });
  }

  removeFromFriendList(username: string): Observable<void> {
    return this.httpClient.delete<void>(`${this.apiUrl}/remove/${encodeURIComponent(username)}`);
  }
}
