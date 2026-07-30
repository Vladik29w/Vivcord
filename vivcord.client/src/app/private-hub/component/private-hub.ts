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
  public readonly selectedFile = signal<File | null>(null);
  public readonly isUploading = signal(false);

  ngOnInit(): void {
    this.chatService.connectToHub();
    this.subscribeToRoute();
    this.subscribeToIncomingMessages();
  }

  ngOnDestroy(): void {
    this.chatService.disconnect();
  }

  public onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile.set(input.files?.[0] ?? null);
  }

  public clearFile(): void {
    this.selectedFile.set(null);
  }

  public async send(text: string): Promise<void> {
    const targetId = this.targetUserId();
    const myId = this.senderId();
    const file = this.selectedFile();

    if (!targetId || (!text.trim() && !file) || !myId) return;

    const tempId = crypto.randomUUID();
    const localPreviewUrl = file ? URL.createObjectURL(file) : undefined;
    const attachmentType = file
      ? (file.type.startsWith('video/') ? 'video' : 'image') as 'image' | 'video'
      : undefined;

    this.messages.update(msgs => [
      ...msgs,
      {
        id: tempId,
        senderId: myId,
        text,
        status: 'sending',
        attachmentUrl: localPreviewUrl,
        attachmentType,
      },
    ]);

    this.selectedFile.set(null);
    this.isUploading.set(true);

    try {
      const realId = await this.chatService.sendMessageWithAttachment(targetId, text, file ?? undefined);
      this.messages.update(msgs =>
        msgs.map(m => {
          if (m.id !== tempId) return m;
          if (localPreviewUrl) URL.revokeObjectURL(localPreviewUrl);
          return { ...m, id: realId, status: 'sent' };
        })
      );
    } catch {
      this.messages.update(msgs =>
        msgs.map(m => (m.id === tempId ? { ...m, status: 'error' } : m))
      );
    } finally {
      this.isUploading.set(false);
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
