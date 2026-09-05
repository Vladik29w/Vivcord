import { Component, inject, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
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
}
