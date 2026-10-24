import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject, map, tap } from 'rxjs';
import { environment } from '@environments/environment';
import {
  ConversationDTO,
  GroupChatDTO,
  CreateGroupChatDTO,
  InviteToGroupRequest,
  AssignAdminRequest,
} from '../dto/conversation.dto';

export function normalizeConversation(conv: ConversationDTO): ConversationDTO {
  const id = String(conv.id);
  const title = conv.title ?? conv.name ?? null;
  const name = conv.title ?? conv.name ?? `Group #${id}`;
  const members = (conv.members ?? []).map(m => ({
    ...m,
    userId: String(m.userId),
    role: m.role ?? 'Member',
    status: m.status ?? 'Joined',
  }));
  const adminMember =
    members.find(m => m.role === 'Admin' && m.status === 'Joined') ??
    members.find(m => m.role === 'Admin');
  const adminId = conv.adminId ?? adminMember?.userId;

  return {
    ...conv,
    id,
    title,
    name,
    members,
    adminId,
  };
}

@Injectable({
  providedIn: 'root',
})
export class GroupManagementService {
  private readonly _http = inject(HttpClient);
  private readonly _groupUrl = `${environment.apiUrl}/Group`;
  private readonly _conversationUrl = `${environment.apiUrl}/Conversation`;

  public readonly groupList = signal<GroupChatDTO[]>([]);
  public readonly pendingGroupInvites = signal<GroupChatDTO[]>([]);

  public readonly groupsChanged$ = new Subject<void>();

  public notifyGroupsChanged(): void {
    this.groupsChanged$.next();
    this.refreshAll();
  }

  public refreshAll(): void {
    this.getMyGroups().subscribe({
      error: err => console.error('failed to refresh groups', err),
    });
    this.getPendingInvites().subscribe({
      error: err => console.error('failed to refresh pending group invites', err),
    });
  }

  public getUserConversations(): Observable<ConversationDTO[]> {
    return this._http
      .get<ConversationDTO[]>(this._conversationUrl, { withCredentials: true })
      .pipe(map(list => list.map(normalizeConversation)));
  }

  public getConversation(conversationId: string): Observable<ConversationDTO> {
    return this._http
      .get<ConversationDTO>(`${this._conversationUrl}/${conversationId}`, { withCredentials: true })
      .pipe(map(normalizeConversation));
  }

  public deleteConversation(conversationId: string): Observable<void> {
    return this._http
      .delete<void>(`${this._conversationUrl}/delete/${conversationId}`, { withCredentials: true })
      .pipe(tap(() => this.notifyGroupsChanged()));
  }

  public getMyGroups(): Observable<GroupChatDTO[]> {
    return this._http
      .get<GroupChatDTO[]>(`${this._groupUrl}/my-groups`, { withCredentials: true })
      .pipe(
        map(list => list.map(normalizeConversation)),
        tap(groups => this.groupList.set(groups))
      );
  }

  public getPendingInvites(): Observable<GroupChatDTO[]> {
    return this._http
      .get<GroupChatDTO[]>(`${this._groupUrl}/pending-invites`, { withCredentials: true })
      .pipe(
        map(list => list.map(normalizeConversation)),
        tap(invites => this.pendingGroupInvites.set(invites))
      );
  }

  public getGroup(conversationId: string): Observable<GroupChatDTO> {
    return this.getConversation(conversationId);
  }

  public createGroup(name: string): Observable<GroupChatDTO> {
    const dto: CreateGroupChatDTO = { name };
    return this._http
      .post<GroupChatDTO>(`${this._groupUrl}/create`, dto, { withCredentials: true })
      .pipe(
        map(normalizeConversation),
        tap(() => this.notifyGroupsChanged())
      );
  }

  public deleteGroup(conversationId: string): Observable<void> {
    return this._http
      .delete<void>(`${this._groupUrl}/delete/${conversationId}`, { withCredentials: true })
      .pipe(tap(() => this.notifyGroupsChanged()));
  }

  public inviteMember(conversationId: string, username: string): Observable<void> {
    const body: InviteToGroupRequest = { username };
    return this._http.post<void>(
      `${this._groupUrl}/invite/${conversationId}`,
      body,
      { withCredentials: true }
    );
  }

  public addMember(conversationId: string, userName: string): Observable<void> {
    return this.inviteMember(conversationId, userName);
  }

  public acceptInvite(conversationId: string): Observable<void> {
    return this._http
      .post<void>(`${this._groupUrl}/accept/${conversationId}`, {}, { withCredentials: true })
      .pipe(tap(() => this.notifyGroupsChanged()));
  }

  public declineInvite(conversationId: string): Observable<void> {
    return this._http
      .post<void>(`${this._groupUrl}/decline/${conversationId}`, {}, { withCredentials: true })
      .pipe(tap(() => this.notifyGroupsChanged()));
  }

  public leaveGroup(conversationId: string): Observable<void> {
    return this._http
      .post<void>(`${this._groupUrl}/leave/${conversationId}`, {}, { withCredentials: true })
      .pipe(tap(() => this.notifyGroupsChanged()));
  }

  public kickMember(conversationId: string, userName: string): Observable<void> {
    return this._http.delete<void>(
      `${this._groupUrl}/kick/${conversationId}?username=${encodeURIComponent(userName)}`,
      { withCredentials: true }
    );
  }

  public removeMember(conversationId: string, userName: string): Observable<void> {
    return this.kickMember(conversationId, userName);
  }

  public assignAdmin(conversationId: string, newAdminUsername: string): Observable<void> {
    const body: AssignAdminRequest = { newAdminUsername };
    return this._http.post<void>(
      `${this._groupUrl}/assign-admin/${conversationId}`,
      body,
      { withCredentials: true }
    );
  }
}
