import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PrivateCallRequest, GroupCallRequest, VoiceCallResponse } from '../dto/voice-chat.dto';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class VoiceCallApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/VoiceChat`;

  initiatePrivateCall(targetUsername: string): Observable<VoiceCallResponse> {
    const body: PrivateCallRequest = { targetUsername };
    return this.http.post<VoiceCallResponse>(`${this.baseUrl}/private-call`, body);
  }

  initiateGroupCall(groupId: number): Observable<VoiceCallResponse> {
    const body: GroupCallRequest = { groupId };
    return this.http.post<VoiceCallResponse>(`${this.baseUrl}/group-call`, body);
  }
}
