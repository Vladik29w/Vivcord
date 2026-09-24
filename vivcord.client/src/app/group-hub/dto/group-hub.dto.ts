import { UserProfileDTO } from '../../profile/dto/profile.dto';

export type { UserProfileDTO };

export interface GroupChatDTO {
  id: number;
  name: string;
  adminId: string;
  voiceRoomId?: string;
  members?: UserProfileDTO[];
}

export interface CreateGroupChatDTO {
  name: string;
}
