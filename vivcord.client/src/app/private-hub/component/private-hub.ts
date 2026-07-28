import { Component, inject, signal, OnInit, OnDestroy, computed, DestroyRef} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { switchMap, tap } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { PrivateHubService } from '../service/private-hub.service';
import { AccountService } from '@account/service/account.service';
import { MessageDTO } from '../../shared/messaging/dto/message.dto';

@Component({
  selector: 'app-private-hub',
  standalone: true,
  templateUrl: './private-hub.html',
  styleUrl: './private-hub.css',
})
export class PrivateHubComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly chatService = inject(PrivateHubService);
  private readonly accountService = inject(AccountService);
  private readonly destroyRef = inject(DestroyRef);

  public readonly senderId = computed(() => this.accountService.currentUser()?.id);

  public readonly currentUserNickname = computed(() => {
    const email = this.accountService.currentUser()?.email;
    return email ? email.split('@')[0] : 'Ви';
  });

  public readonly targetUserId = signal<string | null>(null);
  public readonly currentUsername = signal<string>('');
  public readonly messages = signal<MessageDTO[]>([]);

  ngOnInit(): void {
    this.chatService.connectToHub();
    this.subscribeToRoute();
    this.subscribeToIncomingMessages();
  }

  ngOnDestroy(): void {
    this.chatService.disconnect();
  }

  public async send(text: string): Promise<void> {
    const targetId = this.targetUserId();
    const myId = this.senderId();

    if (!targetId || !text.trim() || !myId) return;

    const tempId = crypto.randomUUID();

    this.messages.update(msgs => [
      ...msgs,
      { id: tempId, senderId: myId, text, status: 'sending' },
    ]);

    try {
      const realId = await this.chatService.sendMessage(targetId, text);
      this.messages.update(msgs =>
        msgs.map(m => (m.id === tempId ? { ...m, id: realId, status: 'sent' } : m))
      );
    } catch {
      this.messages.update(msgs =>
        msgs.map(m => (m.id === tempId ? { ...m, status: 'error' } : m))
      );
    }
  }

  private subscribeToRoute(): void {
    this.route.paramMap
      .pipe(
        tap(params => {
          const username = params.get('username');
          if (username) {
            this.currentUsername.set(username);
            localStorage.setItem('lastChat', username);
          }
        }),
        switchMap(params => {
          const username = params.get('username') ?? '';
          return this.chatService.loadUserProfile(username);
        }),
        tap(profile => this.targetUserId.set(profile.id)),
        switchMap(profile => this.chatService.loadChatHistory(profile.id)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: history => this.messages.set(history),
        error: err => console.error('[PrivateHubComponent] Failed to load chat:', err),
      });
  }

  private subscribeToIncomingMessages(): void {
    this.chatService.messageReceived$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(msg => {
        if (msg.senderId === this.targetUserId()) {
          const fullMsg: MessageDTO = {
            ...msg,
            id: msg.id ?? crypto.randomUUID(),
            status: 'sent',
          };
          this.messages.update(m => [...m, fullMsg]);
        } else {
          console.log('[PrivateHubComponent] New message from:', msg.senderId); // TODO: toast
        }
      });
  }
}
