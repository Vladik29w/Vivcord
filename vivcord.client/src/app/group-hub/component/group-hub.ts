import { Component, inject, signal, OnInit, OnDestroy, computed, DestroyRef, ChangeDetectionStrategy, input, effect, ViewChild, ElementRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { GroupHubService } from '../service/group-hub.service';
import { GroupManagementService } from '../service/group-management.service';
import { AccountService } from '@account/service/account.service';
import { MessageDTO } from '../../shared/messaging/dto/message.dto';
import { GroupChatDTO } from '../dto/group-hub.dto';
import { VoiceCallApiService } from '../../voice-chat/service/voice-call-api.service';
import { LiveKitService } from '../../voice-chat/service/live-kit.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-group-hub',
  standalone: true,
  templateUrl: './group-hub.html',
  styleUrl: './group-hub.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupHubComponent implements OnInit, OnDestroy {
  @ViewChild('messagesViewport') private messagesViewport?: ElementRef<HTMLElement>;

  private readonly router = inject(Router);
  private readonly groupService = inject(GroupHubService);
  private readonly groupManagement = inject(GroupManagementService);
  private readonly accountService = inject(AccountService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly voiceCallApi = inject(VoiceCallApiService);
  public readonly livekitService = inject(LiveKitService);

  public readonly groupIdParam = input<string | undefined>(undefined, { alias: 'groupId' });
  public readonly groupId = computed(() => {
    const raw = this.groupIdParam();
    return raw ? Number(raw) : null;
  });

  public readonly isStartingCall = signal(false);
  public readonly isCallActive = computed(() => this.livekitService.isConnected());

  public readonly senderId = computed(() => this.accountService.currentUser()?.id);
  public readonly currentUserNickname = computed(() => {
    const user = this.accountService.currentUser();
    return user?.displayName || (user?.email ? user.email.split('@')[0] : 'yourname');
  });

  public readonly groupInfo = signal<GroupChatDTO | null>(null);
  public readonly messages = signal<MessageDTO[]>([]);
  public readonly selectedFile = signal<File | null>(null);
  public readonly isUploading = signal(false);

  public readonly groupAvatarInitials = computed(() => {
    const name = this.groupInfo()?.name;
    return (name ? name.substring(0, 2) : 'GP').toUpperCase();
  });

  public readonly myAvatarInitials = computed(() => {
    const name = this.currentUserNickname();
    return (name ? name.substring(0, 2) : 'ME').toUpperCase();
  });

  public readonly myProfilePictureUrl = computed(() => this.accountService.currentUser()?.profilePictureUrl ?? null);

  public readonly isAdmin = computed(() => {
    const info = this.groupInfo();
    const me = this.senderId();
    return info && me ? info.adminId === me : false;
  });

  constructor() {
    effect(() => {
      this.messages();
      this.scrollToBottom();
    });

    effect(() => {
      const id = this.groupId();
      if (!id) return;

      this.groupService.loadGroupHistory(id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: history => {
            this.messages.set(history);
            this.scrollToBottom();
          },
          error: err => console.error('[GroupHubComponent] Failed to load history:', err),
        });

      this.groupManagement.getGroup(id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: group => this.groupInfo.set(group),
          error: err => console.error('[GroupHubComponent] Failed to load group info:', err),
        });
    });
  }

  ngOnInit(): void {
    this.groupService.connectToHub();
    this.subscribeToIncomingMessages();
  }

  ngOnDestroy(): void {
    this.groupService.disconnect();
  }

  public handleCallAction(): void {
    if (this.isCallActive()) {
      this.livekitService.disconnect();
      return;
    }

    const gId = this.groupId();
    if (!gId || this.isStartingCall()) return;

    this.isStartingCall.set(true);
    this.voiceCallApi.initiateGroupCall(gId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: async ({ roomId, token }) => {
          try {
            const title = this.groupInfo()?.name || `Group #${gId}`;
            await this.livekitService.connect(environment.liveKitUrl, token, roomId, title);
          } catch (err) {
            console.error('[GroupHub] Voice call connection failed:', err);
          } finally {
            this.isStartingCall.set(false);
          }
        },
        error: err => {
          console.error('[GroupHub] Voice call failed:', err);
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
    const gId = this.groupId();
    const myId = this.senderId();
    const file = this.selectedFile();

    if (!gId || (!text.trim() && !file) || !myId) return;

    const tempId = crypto.randomUUID();
    const localPreviewUrl = file ? URL.createObjectURL(file) : undefined;
    const attachmentType = file
      ? (file.type.startsWith('video/') ? 'video' : 'image') as 'image' | 'video'
      : undefined;

    this.messages.update(msgs => [
      ...msgs,
      { id: tempId, senderId: myId, text, status: 'sending', attachmentUrl: localPreviewUrl, attachmentType },
    ]);

    this.selectedFile.set(null);
    this.isUploading.set(true);

    try {
      const realId = await this.groupService.sendMessageWithAttachment(
        gId.toString(),
        text,
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

  public addMember(userName: string): void {
    const gId = this.groupId();
    if (!gId || !userName.trim()) return;

    this.groupManagement.addMember(gId, userName)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => console.log(`[GroupHub] Added ${userName}`),
        error: err => console.error('[GroupHub] Add member failed:', err),
      });
  }

  private subscribeToIncomingMessages(): void {
    this.groupService.messageReceived$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(msg => {
        // Skip echo of own messages — they are already added optimistically in send()
        if (msg.senderId === this.senderId()) return;

        const fullMsg: MessageDTO = {
          ...msg,
          id: msg.id ?? crypto.randomUUID(),
          status: 'sent',
        };
        this.messages.update(m => [...m, fullMsg]);
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
