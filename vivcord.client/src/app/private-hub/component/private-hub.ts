import { Component, inject, signal, OnInit, OnDestroy, computed, DestroyRef, ChangeDetectionStrategy, input, effect, ViewChild, ElementRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { switchMap, tap } from 'rxjs';
import { Router } from '@angular/router';
import { PrivateHubService } from '../service/private-hub.service';
import { AccountService } from '@account/service/account.service';
import { MessageDTO } from '../../shared/messaging/dto/message.dto';
import { VoiceCallApiService } from '../../voice-chat/service/voice-call-api.service';
import { LiveKitService } from '../../voice-chat/service/live-kit.service';
import { ToastService } from '../../shared/toast/service/toast.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-private-hub',
  standalone: true,
  templateUrl: './private-hub.html',
  styleUrl: './private-hub.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PrivateHubComponent implements OnInit, OnDestroy {
  @ViewChild('messagesViewport') private messagesViewport?: ElementRef<HTMLElement>;

  private readonly router = inject(Router);
  private readonly chatService = inject(PrivateHubService);
  private readonly accountService = inject(AccountService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly voiceCallApi = inject(VoiceCallApiService);
  public readonly livekitService = inject(LiveKitService);

  public readonly usernameParam = input<string | undefined>(undefined, { alias: 'username' });

  public readonly isStartingCall = signal(false);

  public readonly isCallActive = computed(() => this.livekitService.isConnected());

  public readonly senderId = computed(() => this.accountService.currentUser()?.id);

  public readonly currentUserNickname = computed(() => {
    const user = this.accountService.currentUser();
    return user?.displayName || (user?.email ? user.email.split('@')[0] : 'yourname');
  });

  public readonly targetUserId = signal<string | null>(null);
  public readonly targetDisplayName = signal<string | null>(null);
  public readonly targetProfilePictureUrl = signal<string | null>(null);
  public readonly currentUsername = computed(() => this.usernameParam() ?? '');
  public readonly messages = signal<MessageDTO[]>([]);
  public readonly selectedFile = signal<File | null>(null);
  public readonly isUploading = signal(false);

  public readonly myProfilePictureUrl = computed(() => this.accountService.currentUser()?.profilePictureUrl ?? null);

  public readonly targetAvatarInitials = computed(() => {
    const name = this.targetDisplayName() || this.currentUsername();
    return (name ? name.substring(0, 2) : '??').toUpperCase();
  });

  public readonly myAvatarInitials = computed(() => {
    const name = this.currentUserNickname();
    return (name ? name.substring(0, 2) : 'ME').toUpperCase();
  });

  constructor() {
    effect(() => {
      this.messages();
      this.scrollToBottom();
    });

    effect(() => {
      const username = this.usernameParam();
      if (!username) return;

      localStorage.setItem('lastChat', username);

      this.chatService.loadUserProfile(username)
        .pipe(
          tap(profile => {
            this.targetUserId.set(profile.id);
            this.targetDisplayName.set(profile.displayName || profile.userName);
            this.targetProfilePictureUrl.set(profile.profilePictureUrl ?? null);
          }),
          switchMap(profile => this.chatService.loadChatHistory(profile.id)),
          takeUntilDestroyed(this.destroyRef)
        )
        .subscribe({
          next: history => {
            this.messages.set(history);
            this.scrollToBottom();
          },
          error: err => console.error('[PrivateHubComponent] Failed to load chat:', err),
        });
    });
  }

  ngOnInit(): void {
    this.chatService.connectToHub();
    this.subscribeToIncomingMessages();
  }

  ngOnDestroy(): void {
    this.chatService.disconnect();
  }

  public handleCallAction(): void {
    if (this.isCallActive()) {
      this.livekitService.disconnect();
      return;
    }

    const username = this.currentUsername();
    if (!username || this.isStartingCall()) return;

    this.isStartingCall.set(true);
    this.voiceCallApi.initiatePrivateCall(username)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: async ({ roomId, token }) => {
          try {
            await this.livekitService.connect(environment.liveKitUrl, token, roomId, username);
          } catch (err) {
            console.error('[PrivateHub] LiveKit connect failed:', err);
          } finally {
            this.isStartingCall.set(false);
          }
        },
        error: err => {
          console.error('[PrivateHub] Voice call failed:', err);
          this.isStartingCall.set(false);
        },
      });
  }

  public isMyMessage(msgSenderId: string): boolean {
    const current = this.senderId();
    return !!current && msgSenderId.toLowerCase() === current.toLowerCase();
  }

  public formatMessageTime(timestamp?: string | Date): string {
    if (!timestamp) {
      const now = new Date();
      return now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }
    const d = new Date(timestamp);
    if (isNaN(d.getTime())) return '';
    return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
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

  private subscribeToIncomingMessages(): void {
    this.chatService.messageReceived$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(msg => {
        const target = this.targetUserId();
        if (target && msg.senderId.toLowerCase() === target.toLowerCase()) {
          const fullMsg: MessageDTO = {
            ...msg,
            id: msg.id ?? crypto.randomUUID(),
            status: 'sent',
          };
          this.messages.update(m => [...m, fullMsg]);
        } else {
          const senderName = msg.senderName || 'Someone';
          this.toastService.show({
            title: senderName,
            message: msg.text || (msg.attachmentType ? `Sent a ${msg.attachmentType}` : 'Sent an attachment'),
            avatarUrl: msg.senderAvatarUrl,
            avatarInitials: senderName.substring(0, 2).toUpperCase(),
            type: 'message',
            onClick: () => {
              if (msg.senderName) {
                this.router.navigate(['/chat', msg.senderName]);
              }
            },
          });
        }
      });
  }

  public scrollToBottom(smooth = false): void {
    setTimeout(() => {
      const el = this.messagesViewport?.nativeElement;
      if (el) {
        el.scrollTo({
          top: el.scrollHeight,
          behavior: smooth ? 'smooth' : 'instant',
        });
      }
    }, 50);
  }
}
