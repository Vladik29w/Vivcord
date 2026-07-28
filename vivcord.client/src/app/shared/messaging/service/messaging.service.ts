import { inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Subject, Observable } from 'rxjs';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { AccountService } from '@account/service/account.service';
import { MessageDTO } from '../dto/message.dto';
import { environment } from '@environments/environment';

export abstract class MessagingService {
  protected readonly _http = inject(HttpClient);
  protected readonly _apiUrl = environment.apiUrl;
  private readonly _accountService = inject(AccountService);

  private _hubConnection?: HubConnection;

  public readonly messageReceived$ = new Subject<MessageDTO>();

  protected connectToHub(hubPath: string): void {
    this._hubConnection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}${hubPath}`, { withCredentials: true })
      .withAutomaticReconnect()
      .build();

    this._hubConnection.start()
      .then(() => console.log(`[MessagingService] Connected to ${hubPath}`))
      .catch(err => console.error('[MessagingService] Connection error:', err));

    this._hubConnection.on(
      'ReceiveMessage',
      (senderId: string, text: string, messageId: number) => {
        this.messageReceived$.next({ id: messageId, status: 'sent', senderId, text });
      }
    );

    this._hubConnection.onclose(async (err) => {
      if (err?.message.includes('404')) {
        this._accountService.refresh().subscribe({
          next: () => this._hubConnection?.start(),
          error: () => console.error('[MessagingService] Token refresh failed:', err),
        });
      }
    });
  }

  public async sendMessage(targetId: string, text: string): Promise<number> {
    if (this._hubConnection?.state !== HubConnectionState.Connected) {
      throw new Error('[MessagingService] Hub is not connected.');
    }

    try {
      return await this._hubConnection.invoke<number>('SendMessage', text, targetId);
    } catch (err) {
      console.error('[MessagingService] sendMessage failed:', err);
      throw err;
    }
  }

  public loadChatHistory(targetId: string): Observable<MessageDTO[]> {
    return this._http.get<MessageDTO[]>(
      `${environment.apiUrl}/Messaging/history/${targetId}`
    );
  }

  public disconnect(): void {
    this._hubConnection?.stop();
  }
}
