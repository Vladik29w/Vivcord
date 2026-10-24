import { Injectable } from '@angular/core';
import { Observable, map, catchError, of } from 'rxjs';
import { MessagingService, UserStatusEvent } from '../../shared/messaging/service/messaging.service';
import { UserProfileDTO } from '../../profile/dto/profile.dto';
import { ConversationDTO } from '../../group-hub/dto/conversation.dto';

export type { UserProfileDTO, UserStatusEvent };//maybe delete

@Injectable({
  providedIn: 'root',
})
export class PrivateHubService extends MessagingService {
  public override connectToHub(): void {
    super.connectToHub('/hubs/chat');
  }

  public loadUserProfile(username: string): Observable<UserProfileDTO> {
    return this._http.get<UserProfileDTO>(
      `${this._apiUrl}/Contact/find/${encodeURIComponent(username)}`,
      { withCredentials: true }
    );
  }

  public findDirectConversation(username: string, targetUserId?: string): Observable<ConversationDTO | null> {
    return this._http
      .get<ConversationDTO[]>(`${this._apiUrl}/Conversation`, { withCredentials: true })
      .pipe(
        map(conversations => {
          const lowerName = username.toLowerCase();
          const lowerId = targetUserId?.toLowerCase();
          const found = conversations.find(
            c =>
              c.type === 'Direct' &&
              c.members?.some(
                m =>
                  m.userName?.toLowerCase() === lowerName ||
                  (lowerId && m.userId?.toLowerCase() === lowerId)
              )
          );
          return found ? { ...found, id: String(found.id) } : null;
        }),
        catchError(() => of(null))
      );
  }
}
