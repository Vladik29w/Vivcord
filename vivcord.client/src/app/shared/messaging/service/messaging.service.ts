import { inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Subject, Observable, firstValueFrom } from 'rxjs';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { AccountService } from '@account/service/account.service';
import { MessageDTO } from '../dto/message.dto';
import { environment } from '@environments/environment';

interface UploadTokenResponse {
  uploadUrl: string;
  blobName: string;
}

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
      (senderId: string, text: string, messageId: number, attachmentUrl?: string, attachmentType?: 'image' | 'video') => {
        this.messageReceived$.next({ id: messageId, status: 'sent', senderId, text, attachmentUrl, attachmentType });
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

  public async sendMessage(
    targetId: string,
    text: string,
    blobName?: string,
    attachmentType?: 'image' | 'video'
  ): Promise<number> {
    if (this._hubConnection?.state !== HubConnectionState.Connected) {
      throw new Error('[MessagingService] Hub is not connected.');
    }

    try {
      return await this._hubConnection.invoke<number>('SendMessage', {
        targetUserId: targetId,
        text,
        attachmentUrl: blobName ?? null,
        attachmentType: attachmentType ?? null,
      });
    } catch (err) {
      console.error('[MessagingService] sendMessage failed:', err);
      throw err;
    }
  }
  public getUploadToken(fileName: string, contentType: string): Observable<UploadTokenResponse> {
    return this._http.post<UploadTokenResponse>(
      `${this._apiUrl}/Media/upload-token`,
      { fileName, contentType },
      { withCredentials: true }
    );
  }

  public uploadToBlob(sasUrl: string, file: File): Observable<void> {
    const headers = new HttpHeaders({
      'x-ms-blob-type': 'BlockBlob',
      'Content-Type': file.type,
    });
    return this._http.put<void>(sasUrl, file, { headers });
  }

  /**
   * Orchestrates the full media send flow:
   * 1. Request SAS upload token from backend
   * 2. PUT file directly to Azure Blob Storage
   * 3. Send SignalR message with blob name
   */
  public async sendMessageWithAttachment(
    targetId: string,
    text: string,
    file?: File
  ): Promise<number> {
    if (!file) {
      return this.sendMessage(targetId, text);
    }

    const attachmentType: 'image' | 'video' = file.type.startsWith('video/') ? 'video' : 'image';

    const tokenResponse = await firstValueFrom(this.getUploadToken(file.name, file.type));
    await firstValueFrom(this.uploadToBlob(tokenResponse.uploadUrl, file));

    return this.sendMessage(targetId, text, tokenResponse.blobName, attachmentType);
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
