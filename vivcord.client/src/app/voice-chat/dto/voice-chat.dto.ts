export interface PrivateCallRequest {
  targetUsername: string;
}

export interface GroupCallRequest {
  groupId: number;
}

export interface VoiceCallResponse {
  roomId: string;
  token: string;
}

export interface VoiceParticipant {
  identity: string;
  isSpeaking: boolean;
}
