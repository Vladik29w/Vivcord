import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { MessagingService } from '../../shared/messaging/service/messaging.service';
import { UserProfileDTO } from '../../profile/dto/profile.dto';

export type { UserProfileDTO };

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
