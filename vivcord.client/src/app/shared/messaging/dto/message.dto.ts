export interface MessageDTO {
  id: string;
  conversationId?: string;
  groupId?: string;
  senderId: string;
  senderName?: string;
  senderAvatarUrl?: string | null;
  text: string;
  status: MessageStatus;
  attachmentUrl?: string;
  attachmentType?: 'image' | 'video';
  timestamp?: string | Date;
  createdAt?: string | Date;
  sentAt?: string | Date;
  editedAt?: string | Date | null;
}

export type MessageStatus = 'sending' | 'sent' | 'error';
