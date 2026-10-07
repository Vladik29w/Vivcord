import { Injectable } from '@angular/core';
import { MessagingService } from '../../shared/messaging/service/messaging.service';
import { Observable } from 'rxjs';
import { MessageDTO } from '../../shared/messaging/dto/message.dto';
import { GroupChatDTO, HubResult } from '../dto/group-hub.dto';
import { environment } from '@environments/environment';

@Injectable({
  providedIn: 'root',
})
export class GroupHubService extends MessagingService {
  public override connectToHub(): void {
    super.connectToHub('/hubs/group');
  }

  protected override getHistoryUrl(groupId: string | number): string {
    return `${environment.apiUrl}/Messaging/group-history/${groupId}`;
  }

  public async joinGroup(groupId: number): Promise<void> {
    const res = await this.invokeHub<HubResult<void>>('JoinGroup', groupId);
    if (res?.isError) {
      throw new Error(res.firstError?.description || 'Failed to join group');
    }
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
    const res = await this.invokeHub<number | HubResult<number>>('SendMessage', {
      groupId: Number(groupId),
      text,
      attachmentUrl: blobName ?? null,
      attachmentType: attachmentType ?? null,
    });

    if (typeof res === 'object' && res?.isError) {
      throw new Error(res.firstError?.description || 'Failed to send message');
    }

    return typeof res === 'number' ? res : (res?.value ?? 0);
  }

  public loadGroupHistory(groupId: number): Observable<MessageDTO[]> {
    return this.loadChatHistory(groupId);
  }
}
