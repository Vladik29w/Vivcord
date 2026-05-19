import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { Subscription } from 'rxjs'
import { PrivateHubService } from '../service/private-hub.service';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { environment } from '@environments/environment';
import { messageDTO } from '../dto/message-dto';
import { AccountService } from '@account/service/account.service';
@Component({
  selector: 'app-private-hub',
  standalone: true,
  templateUrl: './private-hub.html',
  styleUrl: './private-hub.css',
})
export class PrivateHubComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private http = inject(HttpClient);
  private chatService = inject(PrivateHubService);
  private accountService = inject(AccountService);

  public senderId = computed(() => this.accountService.currentUser()?.id);
  public targetUserId = signal<string | null>(null);
  public currentUsername = signal<string>('');
  public messages = signal<messageDTO[]>([]);
  private messageSub?: Subscription;
  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const username = params.get('username');
      if (username) {
        this.currentUsername.set(username);
        this.loadUserProfile(username);
      }
    });

    this.chatService.connectToHub();
    
    this.messageSub = this.chatService.messageReceived$.subscribe(msg => {
      if (msg.senderId == this.targetUserId()) {
        const fullMsg: messageDTO = {
          ...msg,
          id: crypto.randomUUID(),
          status: "sending",
          senderId: this.senderId.toString()
        };
        this.messages.update(m => [...m, fullMsg]);
      }
      else {
        console.log("new message from: ", msg.senderId)//TODO: make toast notification
      }
    })
  }

  private loadUserProfile(username: string) {
    this.http.get<{ id: string, userName: string }>(`${environment.apiUrl}/Messaging/find/${username}`)
      .subscribe({
        next: (user) => {
          this.targetUserId.set(user.id);
          this.loadChatHistory(user.id);
        },
        error: () => console.error('User not found')
      });
  }
  private loadChatHistory(targetId: string) {
    this.http.get<messageDTO[]>(`${environment.apiUrl}/Messaging/history/${targetId}`)
      .subscribe({
        next: (history) => {
          this.messages.set(history);
        },
        error: (err) => console.error(err)
      });
  }
  public async send(text: string) {
    const id = this.targetUserId();
    if (id && text.trim()) {
      const tempId = crypto.randomUUID();
      this.messages.update((msgs) => [
        ...msgs,
        {
          id: tempId,
          senderId: this.senderId.toString(),
          text: text,
          status: 'sending'
        }
      ]);

      try {
        const realId = await this.chatService.sendMessage(id, text);
        if (realId) {
          this.messages.update((msg) =>
            msg.map((m) => m.id === tempId ? { ...m, id: realId, status: 'sent' } : m
            )
          );
        }
      }
      catch(err) {
        console.error('Failed to send message:', err);
        this.messages.update((msg) =>
          msg.map((m) => tempId ? { ...m, status: 'error' } : m
          )
        )
      }
    }
  }
}
