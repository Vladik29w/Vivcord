export interface GenerateTokenRequest {
  roomName: string;
  userName: string;
}
export interface VoiceTokenResponse {
  token: string;
}
export interface VoiceParticipant {
  identity: string;
  isSpeaking: boolean;
}
