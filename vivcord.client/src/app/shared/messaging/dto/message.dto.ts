export interface MessageDTO {
  id: string | number;
  senderId: string;
  senderName?: string;
  text: string;
  status: MessageStatus;
  attachmentUrl?: string;
  attachmentType?: 'image' | 'video';
}

export type MessageStatus = 'sending' | 'sent' | 'error';
