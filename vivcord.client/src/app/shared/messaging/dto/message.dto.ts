export interface MessageDTO {
  id: string | number;
  senderId: string;
  senderName?: string;
  senderAvatarUrl?: string | null;
  text: string;
  status: MessageStatus;
  attachmentUrl?: string;
  attachmentType?: 'image' | 'video';
  timestamp?: string | Date;
  createdAt?: string | Date;
}

export type MessageStatus = 'sending' | 'sent' | 'error';

