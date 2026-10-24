import { inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Subject, Observable, firstValueFrom, map } from 'rxjs';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { AccountService } from '@account/service/account.service';
import { MessageDTO } from '../dto/message.dto';
import { HubResult } from '../../../group-hub/dto/conversation.dto';
import { environment } from '@environments/environment';
import { UploadTokenResponse } from '../../../profile/dto/profile.dto';

export interface UserStatusEvent {
  userId: string;
  isOnline: boolean;
}

export abstract class MessagingService {
  protected readonly _http = inject(HttpClient);
  protected readonly _apiUrl = environment.apiUrl;
  private readonly _accountService = inject(AccountService);

  private static _sharedHubConnection?: HubConnection;
  private static _connectPromise?: Promise<void>;
  private static readonly _sharedMessageReceived$ = new Subject<MessageDTO>();
  private static readonly _sharedUserStatusChanged$ = new Subject<UserStatusEvent>();
  private static readonly _sharedFriendsUpdated$ = new Subject<void>();
  private static readonly _sharedGroupsUpdated$ = new Subject<string | undefined>();

  protected get _hubConnection(): HubConnection | undefined {
    return MessagingService._sharedHubConnection;
  }

  public get messageReceived$(): Subject<MessageDTO> {
    return MessagingService._sharedMessageReceived$;
  }

  public get userStatusChanged$(): Subject<UserStatusEvent> {
    return MessagingService._sharedUserStatusChanged$;
  }

  public get friendsUpdated$(): Subject<void> {
    return MessagingService._sharedFriendsUpdated$;
  }

  public get groupsUpdated$(): Subject<string | undefined> {
    return MessagingService._sharedGroupsUpdated$;
  }

  public connectToHub(hubPath = '/hubs/chat'): void {
    const existing = MessagingService._sharedHubConnection;
    if (
      existing &&
      (existing.state === HubConnectionState.Connected ||
        existing.state === HubConnectionState.Connecting ||
        existing.state === HubConnectionState.Reconnecting)
    ) {
      return;
    }

    const connection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}${hubPath}`, { withCredentials: true })
      .withAutomaticReconnect()
      .build();

    MessagingService._sharedHubConnection = connection;

    connection.on(
      'ReceiveMessage',
      (
        senderId: string,
        text: string,
        messageId: string | number,
        attachmentUrl?: string,
        attachmentType?: 'image' | 'video',
        senderName?: string,
        senderAvatarUrl?: string,
        sentAt?: string,
        conversationId?: string | number
      ) => {
        const convIdStr = conversationId != null ? String(conversationId) : undefined;
        const dateObj = sentAt ? new Date(sentAt) : new Date();
        MessagingService._sharedMessageReceived$.next({
          id: String(messageId),
          conversationId: convIdStr,
          groupId: convIdStr,
          status: 'sent',
          senderId: String(senderId),
          senderName,
          senderAvatarUrl,
          text,
          attachmentUrl,
          attachmentType,
          createdAt: dateObj,
          sentAt: dateObj,
          timestamp: dateObj,
        });
      }
    );

    connection.on('UserStatusChanged', (userId: string, isOnline: boolean) => {
      MessagingService._sharedUserStatusChanged$.next({ userId: String(userId), isOnline });
    });

    connection.on('FriendsUpdated', () => {
      MessagingService._sharedFriendsUpdated$.next();
    });

    connection.on('GroupsUpdated', (conversationId?: string | number) => {
      MessagingService._sharedGroupsUpdated$.next(
        conversationId != null ? String(conversationId) : undefined
      );
    });

    connection.onclose(async err => {
      if (err?.message?.includes('401') || err?.message?.includes('404')) {
        this._accountService.refresh().subscribe({
          next: () => {
            MessagingService._connectPromise = connection.start();
          },
          error: refreshErr => console.error('[MessagingService] Token refresh failed:', refreshErr),
        });
      }
    });

    MessagingService._connectPromise = connection
      .start()
      .then(() => console.log(`[MessagingService] Connected to ${hubPath}`))
      .catch(err => console.error('[MessagingService] Connection error:', err));
  }

  protected async ensureConnected(): Promise<HubConnection> {
    if (!MessagingService._sharedHubConnection || MessagingService._sharedHubConnection.state === HubConnectionState.Disconnected) {
      this.connectToHub('/hubs/chat');
    }

    if (MessagingService._sharedHubConnection?.state === HubConnectionState.Connecting && MessagingService._connectPromise) {
      await MessagingService._connectPromise;
    }

    if (!MessagingService._sharedHubConnection || MessagingService._sharedHubConnection.state !== HubConnectionState.Connected) {
      throw new Error('[MessagingService] Hub is not connected.');
    }

    return MessagingService._sharedHubConnection;
  }

  protected async invokeHub<T>(methodName: string, ...args: unknown[]): Promise<T> {
    const conn = await this.ensureConnected();
    return conn.invoke<T>(methodName, ...args);
  }

  public async joinConversation(conversationId: string | number): Promise<void> {
    try {
      const idStr = String(conversationId);
      const arg = Number.isSafeInteger(Number(idStr)) ? Number(idStr) : idStr;
      const res = await this.invokeHub<HubResult<unknown>>('JoinConversation', arg);
      if (res && typeof res === 'object' && res.isError) {
        throw new Error(res.firstError?.description || 'Failed to join conversation');
      }
    } catch (err) {
      console.error('[MessagingService] joinConversation failed:', err);
    }
  }

  public async sendMessage(
    conversationId: string,
    text: string,
    blobName?: string,
    attachmentType?: 'image' | 'video'
  ): Promise<string> {
    try {
      const res = await this.invokeHub<string | number | HubResult<string | number>>('SendMessage', {
        conversationId: String(conversationId),
        text,
        attachmentUrl: blobName ?? null,
        attachmentType: attachmentType ?? null,
      });

      if (typeof res === 'object' && res !== null) {
        if (res.isError) {
          throw new Error(res.firstError?.description || 'Failed to send message');
        }
        return res.value != null ? String(res.value) : '';
      }

      return String(res ?? '');
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
      'x-ms-blob-cache-control': 'public, max-age=31536000, immutable',
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
    conversationId: string,
    text: string,
    file?: File
  ): Promise<string> {
    if (!file) {
      return this.sendMessage(conversationId, text);
    }

    const attachmentType: 'image' | 'video' = file.type.startsWith('video/') ? 'video' : 'image';

    const tokenResponse = await firstValueFrom(this.getUploadToken(file.name, file.type));
    await firstValueFrom(this.uploadToBlob(tokenResponse.uploadUrl, file));

    return this.sendMessage(conversationId, text, tokenResponse.blobName, attachmentType);
  }

  protected getHistoryUrl(conversationOrTargetId: string | number): string {
    return `${environment.apiUrl}/Messaging/history/${conversationOrTargetId}`;
  }

  public loadChatHistory(conversationOrTargetId: string | number): Observable<MessageDTO[]> {
    return this._http.get<MessageDTO[]>(this.getHistoryUrl(conversationOrTargetId)).pipe(
      map(messages =>
        messages.map(m => {
          const convId =
            m.conversationId != null
              ? String(m.conversationId)
              : m.groupId != null
                ? String(m.groupId)
                : undefined;
          const dateVal = m.sentAt ?? m.createdAt ?? m.timestamp;
          const parsedDate = dateVal ? new Date(dateVal) : new Date();
          return {
            ...m,
            id: String(m.id),
            conversationId: convId,
            groupId: convId,
            status: 'sent' as const,
            timestamp: parsedDate,
            createdAt: parsedDate,
            sentAt: parsedDate,
          };
        })
      )
    );
  }

  public disconnect(): void {
    // Keep shared hub connection open during in-app route navigation;
    // call forceDisconnect() only when full teardown is desired.
  }

  public forceDisconnect(): void {
    MessagingService._sharedHubConnection?.stop();
    MessagingService._sharedHubConnection = undefined;
    MessagingService._connectPromise = undefined;
  }
}
