import { Injectable, signal, inject } from '@angular/core';
import { Subject } from 'rxjs';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { HttpClient } from '@angular/common/http';
import { AccountService } from '@account/service/account.service';
import { messageDTO } from '../dto/message-dto';
import { environment } from '@environments/environment';
@Injectable({
  providedIn: 'root',
})
export class PrivateHubService {
  private _hubConntection?: HubConnection;
  private _accountService = inject(AccountService);
  private _http = inject(HttpClient);
  public messageReceived$ = new Subject<messageDTO>()

  public connectToHub() {
    this._hubConntection = new HubConnectionBuilder()
      .withUrl(`https://localhost:7048/hubs/private`, {
        withCredentials: true
      })
      .withAutomaticReconnect()
      .build();

    this._hubConntection.start()
      .then(() => console.log("Connected to hub"))
      .catch(err => console.log(err));

    this._hubConntection.on("ReciveMessage", (senderId: string, text: string, messageId: number) => {
      this.messageReceived$.next({ id: messageId, status: "sent", senderId, text });
    })
    this._hubConntection?.onclose(async (err) => {
      if (err && err.message.includes('404')) {
        this._accountService.refresh().subscribe({
          next: () => {
            this._hubConntection?.start();
          },
          error: () => {
            console.log("refresh error", err);
          }
        })
      }
    })
  }
  public async sendMessage(targetUser: string, text: string): Promise<number> {
    if (this._hubConntection?.state == HubConnectionState.Connected) {
      try {
        const messageId = await this._hubConntection.invoke<number>("SendMessage", text, targetUser);
        return messageId;
      }
      catch (err) {
        console.log(err); 
        throw err;
      }
    }
    else {
      console.log("Not connected to hub");
      throw new Error("Not connected to hub");
    }
  }

  public loadUserProfile(username: string, onSuccess: (userId: string) => void) {
    this._http.get<{ id: string, userName: string }>(`${environment.apiUrl}/Contact/find/${username}`)
      .subscribe({
        next: (user) => {
          onSuccess(user.id);
        },
        error: () => console.error('User not found')
      });
  }

  public loadChatHistory(targetId: string, onSuccess: (history: messageDTO[]) => void) {
    this._http.get<messageDTO[]>(`${environment.apiUrl}/Messaging/history/${targetId}`)
      .subscribe({
        next: (history) => {
          onSuccess(history);
        },
        error: (err) => console.error(err)
      });
  }
}
