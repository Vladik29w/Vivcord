import {
  Component,
  inject,
  signal,
  OnInit,
  computed,
  DestroyRef,
  ChangeDetectionStrategy,
  input,
  effect,
  ViewChild,
  ElementRef,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin, switchMap, tap, of } from 'rxjs';
import { Router } from '@angular/router';
import { PrivateHubService } from '../service/private-hub.service';
import { AccountService } from '@account/service/account.service';
import { MessageDTO } from '../../shared/messaging/dto/message.dto';
import { VoiceCallApiService } from '../../voice-chat/service/voice-call-api.service';
import { LiveKitService } from '../../voice-chat/service/live-kit.service';
import { ToastService } from '../../shared/toast/service/toast.service';
import { environment } from '../../../environments/environment';
import { KlipyComponent } from '../../shared/messaging/klipy/component/klipy';

@Component({
  selector: 'app-private-hub',
  standalone: true,
  imports: [KlipyComponent],
  templateUrl: './private-hub.html',
  styleUrl: './private-hub.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PrivateHubComponent implements OnInit {
  @ViewChild('messagesViewport') private messagesViewport?: ElementRef<HTMLElement>;
  @ViewChild('msgInput') private msgInput?: ElementRef<HTMLInputElement>;

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

  public readonly conversationId = signal<string | null>(null);
  public readonly canSendMessages = signal<boolean>(true);
  public readonly targetUserId = signal<string | null>(null);
  public readonly targetDisplayName = signal<string | null>(null);
  public readonly targetProfilePictureUrl = signal<string | null>(null);
  public readonly isTargetOnline = signal<boolean>(false);
  public readonly currentUsername = computed(() => this.usernameParam() ?? '');
  public readonly messages = signal<MessageDTO[]>([]);
  public readonly messageText = signal('');
  public readonly selectedFile = signal<File | null>(null);
  public readonly isUploading = signal(false);
  public readonly showGifPicker = signal(false);

  public readonly myProfilePictureUrl = computed(
    () => this.accountService.currentUser()?.profilePictureUrl ?? null
  );

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

      localStorage.setItem('lastChat', username);//loop?
      this.conversationId.set(null);

      this.chatService
        .loadUserProfile(username)
        .pipe(
          tap(profile => {
            const targetId = profile.id || profile.userId || '';
            this.targetUserId.set(targetId);
            this.targetDisplayName.set(profile.displayName || profile.userName);
            this.targetProfilePictureUrl.set(profile.profilePictureUrl ?? null);
            this.isTargetOnline.set(profile.isOnline ?? false);
          }),
          switchMap(profile => {
            const targetId = profile.id || profile.userId || '';
            return forkJoin({
              conv: this.chatService.findDirectConversation(username, targetId),
              history: targetId ? this.chatService.loadChatHistory(targetId) : of([] as MessageDTO[]),
            });
          }),
          takeUntilDestroyed(this.destroyRef)
        )
        .subscribe({
          next: ({ conv, history }) => {
            if (!conv) {
              // User is not an active member of this direct conversation (e.g. removed friend or not friends)
              this.router.navigate(['/']);
              return;
            }
            const resolvedConvId = conv.id;
            const bothJoined =
              conv.members?.length === 2 && conv.members.every(m => m.status === 'Joined');
            this.conversationId.set(resolvedConvId);
            this.canSendMessages.set(bothJoined);
            if (!bothJoined) {
              this.isTargetOnline.set(false);
            }
            if (resolvedConvId) {
              this.chatService.joinConversation(resolvedConvId);
            }
            this.messages.set(history);
            this.scrollToBottom();
          },
          error: err => {
            console.error('[PrivateHubComponent] Failed to load chat:', err);
            this.router.navigate(['/']);
          },
        });
    });
  }

  ngOnInit(): void {
    this.chatService.connectToHub();
    this.subscribeToIncomingMessages();

    this.chatService.userStatusChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ userId, isOnline }) => {
        const currentTargetId = this.targetUserId();
        if (currentTargetId && currentTargetId.toLowerCase() === userId.toLowerCase()) {
          this.isTargetOnline.set(isOnline);
        }
      });

    this.chatService.friendsUpdated$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        const username = this.usernameParam();
        const targetId = this.targetUserId();
        if (!username) return;
        this.chatService
          .findDirectConversation(username, targetId ?? undefined)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe(conv => {
            if (!conv) {
              this.router.navigate(['/']);
              return;
            }
            const bothJoined =
              conv.members?.length === 2 && conv.members.every(m => m.status === 'Joined');
            this.conversationId.set(conv.id);
            this.canSendMessages.set(bothJoined);
            if (!bothJoined) {
              this.isTargetOnline.set(false);
            }
            this.chatService.joinConversation(conv.id);
          });
      });
  }

  public handleCallAction(): void {
    if (this.isCallActive()) {
      this.livekitService.disconnect();
      return;
    }

    const convId = this.conversationId();
    const username = this.currentUsername();
    if (!convId || this.isStartingCall()) return;

    this.isStartingCall.set(true);
    this.voiceCallApi
      .initiatePrivateCall(convId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: async ({ roomId, token }) => {
          try {
            const callTitle = this.targetDisplayName() || username;
            await this.livekitService.connect(environment.liveKitUrl, token, roomId, callTitle);
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

  public toggleGifPicker(): void {
    this.showGifPicker.update(v => !v);
  }

  public closeGifPicker(): void {
    this.showGifPicker.set(false);
  }

  public async sendGif(gifUrl: string): Promise<void> {
    const convId = this.conversationId();
    const myId = this.senderId();
    if (!convId || !myId) return;

    const tempId = crypto.randomUUID();
    this.messages.update(msgs => [
      ...msgs,
      {
        id: tempId,
        conversationId: convId,
        senderId: myId,
        text: '',
        status: 'sending',
        attachmentUrl: gifUrl,
        attachmentType: 'image' as const,
        timestamp: new Date(),
        createdAt: new Date(),
      },
    ]);

    this.showGifPicker.set(false);

    try {
      const realId = await this.chatService.sendMessage(convId, '', gifUrl, 'image');
      this.messages.update(msgs =>
        msgs.map(m => (m.id === tempId ? { ...m, id: realId, status: 'sent' } : m))
      );
    } catch {
      this.messages.update(msgs =>
        msgs.map(m => (m.id === tempId ? { ...m, status: 'error' } : m))
      );
    }
  }

  public async send(text?: string): Promise<void> {
    const rawText = text !== undefined ? text : this.messageText();
    const convId = this.conversationId();
    const myId = this.senderId();
    const file = this.selectedFile();

    if (!convId || (!rawText.trim() && !file) || !myId) return;

    this.messageText.set('');
    if (this.msgInput?.nativeElement) {
      this.msgInput.nativeElement.value = '';
    }

    const tempId = crypto.randomUUID();
    const localPreviewUrl = file ? URL.createObjectURL(file) : undefined;
    const attachmentType = file
      ? ((file.type.startsWith('video/') ? 'video' : 'image') as 'image' | 'video')
      : undefined;

    this.messages.update(msgs => [
      ...msgs,
      {
        id: tempId,
        conversationId: convId,
        senderId: myId,
        text: rawText,
        status: 'sending',
        attachmentUrl: localPreviewUrl,
        attachmentType,
        timestamp: new Date(),
        createdAt: new Date(),
      },
    ]);

    this.selectedFile.set(null);
    this.isUploading.set(true);

    try {
      const realId = await this.chatService.sendMessageWithAttachment(
        convId,
        rawText,
        file ?? undefined
      );
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
        // Skip echo of own messages — already added optimistically in send()
        if (this.isMyMessage(msg.senderId)) return;

        const currentConvId = this.conversationId();
        const target = this.targetUserId();
        const isCurrentConversation = currentConvId
          ? msg.conversationId === currentConvId
          : !!target && msg.senderId.toLowerCase() === target.toLowerCase();

        if (isCurrentConversation) {
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
            message:
              msg.text ||
              (msg.attachmentType ? `Sent a ${msg.attachmentType}` : 'Sent an attachment'),
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
