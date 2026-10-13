import { Component, inject, computed, OnInit, ChangeDetectionStrategy, signal, HostListener } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { LiveKitService } from '../../service/live-kit.service';
import { AccountService } from '@account/service/account.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-voice-chat',
  standalone: true,
  templateUrl: './voice-chat.html',
  styleUrl: './voice-chat.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VoiceChatComponent implements OnInit {
  protected readonly livekitService = inject(LiveKitService);
  protected readonly accountService = inject(AccountService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly isQualityModalOpen = signal(false);
  protected readonly selectedResolution = signal<'720p' | '1080p'>('1080p');
  protected readonly selectedFrameRate = signal<30 | 60>(30);
  protected readonly includeAudio = signal<boolean>(true);

  protected readonly localDisplayName = computed(() => {
    const user = this.accountService.currentUser();
    return user?.displayName || (user?.email ? user.email.split('@')[0] : 'You');
  });

  protected readonly localAvatarInitials = computed(() => {
    const name = this.localDisplayName();
    return (name ? name.substring(0, 2) : 'ME').toUpperCase();
  });

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const roomId = params.get('roomId');
    const token = params.get('token');

    // If accessed via standalone route with query params, connect and redirect home
    if (roomId && token && !this.livekitService.isConnected()) {
      this.livekitService.connect(environment.liveKitUrl, token, roomId).then(() => {
        this.router.navigate(['/']);
      }).catch(err => {
        console.error('Failed to connect from route:', err);
      });
    }
  }

  async leave(): Promise<void> {
    await this.livekitService.disconnect();
  }

  watchScreen(): void {
    this.router.navigate(['/voice-chat']);
  }

  onScreenShareClick(): void {
    if (this.livekitService.isScreenSharing()) {
      this.livekitService.stopScreenShare();
    } else {
      this.openQualityModal();
    }
  }

  openQualityModal(): void {
    this.selectedResolution.set('1080p');
    this.selectedFrameRate.set(30);
    this.includeAudio.set(true);
    this.isQualityModalOpen.set(true);
  }

  closeQualityModal(): void {
    this.isQualityModalOpen.set(false);
  }

  async confirmStartScreenShare(): Promise<void> {
    const resolution = this.selectedResolution();
    const frameRate = this.selectedFrameRate();
    const audio = this.includeAudio();
    this.closeQualityModal();
    try {
      await this.livekitService.startScreenShare(resolution, frameRate, audio);
    } catch (err) {
      console.error('Failed to start screen share:', err);
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.isQualityModalOpen()) {
      this.closeQualityModal();
    }
  }
}
