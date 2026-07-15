import { Component, inject, input, signal, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { lastValueFrom } from 'rxjs';
import { LiveKitService } from '../../service/live-kit.service';
import { VoiceTokenResponse, GenerateTokenRequest } from '../../dto/voice-chat.dto';
import { environment } from '../../../../environments/environment';
import { AccountService } from '../../../account/service/account.service';
@Component({
  selector: 'app-voice-chat',
  imports: [],
  templateUrl: './voice-chat.html',
  styleUrl: './voice-chat.css',
})
export class VoiceChatComponent implements OnDestroy {
  readonly roomName = input.required<string>();

  protected readonly livekitService = inject(LiveKitService);
  private readonly http = inject(HttpClient);
  private readonly accountService = inject(AccountService);
  protected readonly isJoining = signal(false);

  async join(): Promise<void> {
    this.isJoining.set(true);
    try {
      const token = await this.fetchToken(this.roomName());
      await this.livekitService.connect(environment.liveKitUrl, token);
    } finally {
      this.isJoining.set(false);
    }
  }

  async leave(): Promise<void> {
    await this.livekitService.disconnect();
  }

  private async fetchToken(roomName: string): Promise<string> {
    try {
      const user = this.accountService.currentUser();
      const identity = user ? user.id : 'unknown';
      const displayName = user ? user.displayName : 'Anonymous';

      const requestBody: GenerateTokenRequest = {
        roomName: roomName,
        identity: identity,
        displayName: displayName
      };

      const res$ = this.http.post<VoiceTokenResponse>(
        `${environment.apiUrl}/VoiceChat/VoiceToken`,
        requestBody
      );

      const data = await lastValueFrom(res$);
      return data.token;
    } catch (err) {
      this.livekitService.error.set(`Failed to fetch token: ${err}`);
      throw err;
    }
  }

  ngOnDestroy(): void {
    this.livekitService.disconnect();
  }
}
