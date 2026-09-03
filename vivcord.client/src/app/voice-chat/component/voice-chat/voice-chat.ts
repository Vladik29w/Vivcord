import { Component, inject, signal, computed, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { LiveKitService } from '../../service/live-kit.service';
import { AccountService } from '@account/service/account.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-voice-chat',
  imports: [],
  templateUrl: './voice-chat.html',
  styleUrl: './voice-chat.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VoiceChatComponent implements OnInit, OnDestroy {
  protected readonly livekitService = inject(LiveKitService);
  protected readonly accountService = inject(AccountService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly roomId = signal<string>('');
  protected readonly isJoining = signal(false);

  protected readonly localDisplayName = computed(() => {
    const user = this.accountService.currentUser();
    return user?.displayName || (user?.email ? user.email.split('@')[0] : 'You');
  });

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const roomId = params.get('roomId');
    const token = params.get('token');

    if (!roomId || !token) {
      this.livekitService.error.set('Missing room ID or token. Please initiate a call from a chat.');
      return;
    }

    this.roomId.set(roomId);
    this.join(token);
  }

  async join(token: string): Promise<void> {
    this.isJoining.set(true);
    try {
      await this.livekitService.connect(environment.liveKitUrl, token);
    } finally {
      this.isJoining.set(false);
    }
  }

  async leave(): Promise<void> {
    await this.livekitService.disconnect();
    this.router.navigate(['/']);
  }

  ngOnDestroy(): void {
    this.livekitService.disconnect();
  }
}
