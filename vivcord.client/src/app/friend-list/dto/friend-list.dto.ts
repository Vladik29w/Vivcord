export interface Friend {
  id: string;
  userName: string;
  displayName?: string | null;
  profilePictureUrl?: string | null;
  isOnline?: boolean;
  conversationId?: string | null;
}

export interface PendingFriendRequest {
  conversationId: string;
  userId: string;
  userName: string;
  displayName?: string | null;
  profilePictureUrl?: string | null;
  createdAt: string;
}
