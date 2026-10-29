export interface VoiceCallRequest {
  conversationId: string;
}

export interface VoiceCallResponse {
  roomId: string;
  token: string;
}

export interface VoiceParticipant {
  identity: string;
  name?: string;
  avatarUrl?: string | null;
  isSpeaking: boolean;
}
