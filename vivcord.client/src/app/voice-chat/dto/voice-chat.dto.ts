export interface GenerateTokenRequest {
  roomName: string;
  identity: string;
}
export interface VoiceTokenResponse {
  token: string;
}
export interface Participant {
  identity: string;
  isSpeaking: boolean;
}
