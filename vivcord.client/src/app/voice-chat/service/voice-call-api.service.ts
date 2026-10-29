import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { VoiceCallResponse } from '../dto/voice-chat.dto';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class VoiceCallApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/VoiceChat`;

  initiateCall(conversationId: string): Observable<VoiceCallResponse> {
    return this.http.post<VoiceCallResponse>(
      `${this.baseUrl}/call/${conversationId}`,
      {},
      { withCredentials: true }
    );
  }

  initiatePrivateCall(conversationId: string): Observable<VoiceCallResponse> {
    return this.initiateCall(conversationId);
  }

  initiateGroupCall(conversationId: string): Observable<VoiceCallResponse> {
    return this.initiateCall(conversationId);
  }
}
