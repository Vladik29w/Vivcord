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
    super.connectToHub('/hubs/chat');
  }

  protected override getHistoryUrl(conversationId: string | number): string {
    return `${environment.apiUrl}/Messaging/history/${conversationId}`;
  }

  public async joinGroup(conversationId: string): Promise<void> {
    await this.joinConversation(conversationId);
  }

  public loadGroupHistory(conversationId: string): Observable<MessageDTO[]> {
    return this.loadChatHistory(conversationId);
  }
}
