import { Component, inject, signal, OnInit, OnDestroy, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { switchMap } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';
import { GroupHubService } from '../service/group-hub.service';
import { GroupManagementService } from '../service/group-management.service';
import { AccountService } from '@account/service/account.service';
import { MessageDTO } from '../../shared/messaging/dto/message.dto';
import { GroupChatDTO } from '../dto/group-hub.dto';
import { VoiceCallApiService } from '../../voice-chat/service/voice-call-api.service';

@Component({
  selector: 'app-group-hub',
  standalone: true,
  templateUrl: './group-hub.html',
})
export class GroupHubComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly groupService = inject(GroupHubService);
  private readonly groupManagement = inject(GroupManagementService);
  private readonly accountService = inject(AccountService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly voiceCallApi = inject(VoiceCallApiService);

  public readonly isStartingCall = signal(false);

  public readonly senderId = computed(() => this.accountService.currentUser()?.id);
  public readonly currentUserNickname = computed(() => {
    const email = this.accountService.currentUser()?.email;
    return email ? email.split('@')[0] : 'You';
  });

  public readonly groupId = signal<number | null>(null);
  public readonly groupInfo = signal<GroupChatDTO | null>(null);
  public readonly messages = signal<MessageDTO[]>([]);
  public readonly selectedFile = signal<File | null>(null);
  public readonly isUploading = signal(false);

  public readonly isAdmin = computed(() => {
    const info = this.groupInfo();
    const me = this.senderId();
    return info && me ? info.adminId === me : false;
  });

  ngOnInit(): void {
    this.groupService.connectToHub();
    this.subscribeToRoute();
    this.subscribeToIncomingMessages();
  }

  ngOnDestroy(): void {
    this.groupService.disconnect();
  }

  public startVoiceCall(): void {
    const gId = this.groupId();
    if (!gId || this.isStartingCall()) return;

    this.isStartingCall.set(true);
    this.voiceCallApi.initiateGroupCall(gId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ roomId, token }) => {
          this.router.navigate(['/voice-chat'], { queryParams: { roomId, token } });
        },
        error: err => {
          console.error('[GroupHub] Voice call failed:', err);
          this.isStartingCall.set(false);
        },
      });
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

  private subscribeToRoute(): void {
    this.route.paramMap
      .pipe(
        switchMap(params => {
          const id = Number(params.get('groupId'));
          this.groupId.set(id);
          return this.groupService.loadGroupHistory(id);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: history => this.messages.set(history),
        error: err => console.error('[GroupHubComponent] Failed to load history:', err),
      });

    this.route.paramMap
      .pipe(
        switchMap(params => {
          const id = Number(params.get('groupId'));
          return this.groupManagement.getGroup(id);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: group => this.groupInfo.set(group),
        error: err => console.error('[GroupHubComponent] Failed to load group info:', err),
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
}
