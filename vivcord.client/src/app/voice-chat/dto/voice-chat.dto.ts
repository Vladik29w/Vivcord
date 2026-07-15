export interface GenerateTokenRequest {
  roomName: string;
  identity: string;
  displayName: string;
}
export interface VoiceTokenResponse {
  token: string;
}
export interface VoiceParticipant {
  identity: string;
  isSpeaking: boolean;
}
