import { Injectable, signal, inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { environment } from '@environments/environment';
import { AccountService } from '../../account/service/account.service';
@Injectable({
  providedIn: 'root',
})
export class PrivateHubService {
  private _hubConntection?: HubConnection;
  private _accountService = inject(AccountService);
  public messages = signal<{ senderId: string, text: string }[]>([]);

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

    this._hubConntection.on("ReciveMessage", (senderId: string, text: string) => {
      this.messages.update(msg => [...msg, { senderId, text }]);
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
  public async sendMessage(targetUser: string, text: string) {
    if (this._hubConntection?.state == HubConnectionState.Connected) {
      try {
        await this._hubConntection.invoke("SendMessage", text, targetUser);
      }
      catch (err) {
        console.log(err);
      }
    }
    else
      console.log("Not connected to hub");
  }
}
