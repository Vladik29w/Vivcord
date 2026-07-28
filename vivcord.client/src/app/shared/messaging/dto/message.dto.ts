export interface MessageDTO {
  id: string | number;
  senderId: string;
  text: string;
  status: MessageStatus;
}

export type MessageStatus = 'sending' | 'sent' | 'error';
