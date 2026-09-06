export interface UserProfileDTO {
  userId: string;
  userName: string;
  displayName: string;
  profilePictureUrl?: string | null;
}

export interface GroupChatDTO {
  id: number;
  name: string;
  adminId: string;
  memberIds: string[];
  voiceRoomId?: string;
  members?: UserProfileDTO[];
}

export interface CreateGroupChatDTO {
  name: string;
}
