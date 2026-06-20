import { Component, inject, signal, OnInit, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PrivateHubService } from '../service/private-hub.service';
import { ActivatedRoute } from '@angular/router';
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
  private chatService = inject(PrivateHubService);
  private accountService = inject(AccountService);
  private destroyRef = inject(DestroyRef);

  public senderId = computed(() => this.accountService.currentUser()?.id);
  public targetUserId = signal<string | null>(null);
  public currentUsername = signal<string>('');
  public currentUserNickname = computed(() => {
    const email = this.accountService.currentUser()?.email;
    return email ? email.split('@')[0] : 'Ви';
  });
  public messages = signal<messageDTO[]>([]);

  ngOnInit() {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      const username = params.get('username');
      if (username) {
        this.currentUsername.set(username);
        localStorage.setItem('lastChat', username);

        this.chatService.loadUserProfile(username, (userId) => {
          this.targetUserId.set(userId);
          this.chatService.loadChatHistory(userId, (history) => {
            this.messages.set(history);
          });
        });
      }
    });

    this.chatService.connectToHub();

    this.chatService.messageReceived$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(msg => {
      if (msg.senderId === this.targetUserId()) {
        const fullMsg: messageDTO = {
          ...msg,
          id: msg.id || crypto.randomUUID(),
          status: "sent"
        };
        this.messages.update(m => [...m, fullMsg]);
      } else {
        console.log("new message from: ", msg.senderId);//TODO: make it toast
      }
    });
  }

  public async send(text: string) {
    const id = this.targetUserId();
    const myId = this.senderId();

    if (id && text.trim() && myId) {
      const tempId = crypto.randomUUID();

      this.messages.update((msgs) => [
        ...msgs,
        {
          id: tempId,
          senderId: myId,
          text: text,
          status: 'sending'
        }
      ]);

      try {
        const realId = await this.chatService.sendMessage(id, text);
        if (realId) {
          this.messages.update((msg) =>
            msg.map((m) => m.id === tempId ? { ...m, id: realId, status: 'sent' } : m)
          );
        }
      } catch (err) {
        console.error('Failed to send message:', err);
        this.messages.update((msg) =>
          msg.map((m) => m.id === tempId ? { ...m, status: 'error' } : m)
        );
      }
    }
  }
}