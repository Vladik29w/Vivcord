import { Injectable } from '@angular/core';
import { MessagingService } from '../../shared/messaging/service/messaging.service';
import { Observable } from 'rxjs';
import { MessageDTO } from '../../shared/messaging/dto/message.dto';
import { environment } from '@environments/environment';

@Injectable({
  providedIn: 'root',
})
export class GroupHubService extends MessagingService {
  public override connectToHub(): void {
    super.connectToHub('/hubs/group');
  }

  public async joinGroup(groupId: number): Promise<void> {
    return this.invokeHub<void>('JoinGroup', groupId);
  }

  /**
   * Sends a group message via SignalR.
   * groupId is passed as string (to match base class signature)
   * but converted to number before sending to the hub.
   */
  public override async sendMessage(
    groupId: string,
    text: string,
    blobName?: string,
    attachmentType?: 'image' | 'video'
  ): Promise<number> {
    return this.invokeHub<number>('SendMessage', {
      groupId: Number(groupId),
      text,
      attachmentUrl: blobName ?? null,
      attachmentType: attachmentType ?? null,
    });
  }

  public loadGroupHistory(groupId: number): Observable<MessageDTO[]> {
    return this._http.get<MessageDTO[]>(
      `${environment.apiUrl}/Messaging/group-history/${groupId}`
    );
  }
}
