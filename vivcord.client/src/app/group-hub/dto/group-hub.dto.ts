import { UserProfileDTO } from '../../profile/dto/profile.dto';

export type { UserProfileDTO };

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
