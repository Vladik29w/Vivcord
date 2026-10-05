import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { MessagingService } from '../../shared/messaging/service/messaging.service';
import { UserProfileDTO } from '../../profile/dto/profile.dto';

export type { UserProfileDTO };

export interface UserStatusEvent {
  userId: string;
  isOnline: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class PrivateHubService extends MessagingService {
  public readonly userStatusChanged$ = new Subject<UserStatusEvent>();

  public override connectToHub(): void {
    super.connectToHub('/hubs/private');

    this._hubConnection?.off('UserStatusChanged');
    this._hubConnection?.on('UserStatusChanged', (userId: string, isOnline: boolean) => {
      this.userStatusChanged$.next({ userId, isOnline });
    });
  }

  public loadUserProfile(username: string): Observable<UserProfileDTO> {
    return this._http.get<UserProfileDTO>(
      `${this._apiUrl}/Contact/find/${username}`
    );
  }
}
