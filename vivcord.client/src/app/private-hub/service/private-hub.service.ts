import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { MessagingService } from '../../shared/messaging/service/messaging.service';

export interface UserProfileDTO {
  id: string;
  userName: string;
  displayName?: string;
}

@Injectable({
  providedIn: 'root',
})
export class PrivateHubService extends MessagingService {
  public override connectToHub(): void {
    super.connectToHub('/hubs/private');
  }

  public loadUserProfile(username: string): Observable<UserProfileDTO> {
    return this._http.get<UserProfileDTO>(
      `${this._apiUrl}/Contact/find/${username}`
    );
  }
}
