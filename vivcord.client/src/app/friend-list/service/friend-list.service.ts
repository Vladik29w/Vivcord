import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject, map, tap } from 'rxjs';
import { environment } from '@environments/environment';
import { Friend, PendingFriendRequest } from '../dto/friend-list.dto';
import { ConversationDTO } from '../../group-hub/dto/conversation.dto';
import { normalizeConversation } from '../../group-hub/service/group-management.service';

@Injectable({
  providedIn: 'root',
})
export class FriendListService {
  private readonly apiUrl = `${environment.apiUrl}/Friend`;
  private readonly httpClient = inject(HttpClient);

  public readonly friendList = signal<Friend[]>([]);
  public readonly incomingRequests = signal<PendingFriendRequest[]>([]);
  public readonly outgoingRequests = signal<PendingFriendRequest[]>([]);

  public readonly friendsChanged$ = new Subject<void>();

  public notifyFriendsChanged(): void {
    this.friendsChanged$.next();
    this.refreshAll();
  }

  public refreshAll(): void {
    this.getFriendList().subscribe({
      error: err => console.error('failed to refresh friend list', err),
    });
    this.getIncomingRequests().subscribe({
      error: err => console.error('failed to refresh incoming requests', err),
    });
    this.getOutgoingRequests().subscribe({
      error: err => console.error('failed to refresh outgoing requests', err),
    });
  }

  public updateFriendOnlineStatus(userId: string, isOnline: boolean): void {
    this.friendList.update(friends =>
      friends.map(f =>
        f.id.toLowerCase() === userId.toLowerCase() ? { ...f, isOnline } : f
      )
    );
  }

  getFriendList(): Observable<Friend[]> {
    return this.httpClient
      .get<Friend[]>(`${this.apiUrl}/list`, { withCredentials: true })
      .pipe(
        map(friends =>
          friends.map(f => ({
            ...f,
            id: String(f.id),
            conversationId: f.conversationId != null ? String(f.conversationId) : null,
          }))
        ),
        tap(friends => this.friendList.set(friends))
      );
  }

  sendFriendRequest(username: string): Observable<ConversationDTO> {
    return this.httpClient
      .post<ConversationDTO>(
        `${this.apiUrl}/send-request`,
        { username },
        { withCredentials: true }
      )
      .pipe(
        map(normalizeConversation),
        tap(() => this.notifyFriendsChanged())
      );
  }

  addFriend(username: string): Observable<ConversationDTO> {
    return this.sendFriendRequest(username);
  }

  acceptFriendRequest(conversationId: string): Observable<void> {
    return this.httpClient
      .post<void>(`${this.apiUrl}/accept/${conversationId}`, {}, { withCredentials: true })
      .pipe(tap(() => this.notifyFriendsChanged()));
  }

  declineFriendRequest(conversationId: string): Observable<void> {
    return this.httpClient
      .post<void>(`${this.apiUrl}/decline/${conversationId}`, {}, { withCredentials: true })
      .pipe(tap(() => this.notifyFriendsChanged()));
  }

  cancelFriendRequest(conversationId: string): Observable<void> {
    return this.httpClient
      .post<void>(`${this.apiUrl}/cancel/${conversationId}`, {}, { withCredentials: true })
      .pipe(tap(() => this.notifyFriendsChanged()));
  }

  removeFromFriendList(username: string): Observable<void> {
    return this.httpClient
      .delete<void>(`${this.apiUrl}/remove/${encodeURIComponent(username)}`, {
        withCredentials: true,
      })
      .pipe(tap(() => this.notifyFriendsChanged()));
  }

  getIncomingRequests(): Observable<PendingFriendRequest[]> {
    return this.httpClient
      .get<PendingFriendRequest[]>(`${this.apiUrl}/incoming-requests`, { withCredentials: true })
      .pipe(
        map(list =>
          list.map(r => ({
            ...r,
            conversationId: String(r.conversationId),
            userId: String(r.userId),
          }))
        ),
        tap(list => this.incomingRequests.set(list))
      );
  }

  getOutgoingRequests(): Observable<PendingFriendRequest[]> {
    return this.httpClient
      .get<PendingFriendRequest[]>(`${this.apiUrl}/outgoing-requests`, { withCredentials: true })
      .pipe(
        map(list =>
          list.map(r => ({
            ...r,
            conversationId: String(r.conversationId),
            userId: String(r.userId),
          }))
        ),
        tap(list => this.outgoingRequests.set(list))
      );
  }
}
