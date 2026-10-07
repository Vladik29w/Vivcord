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

export interface HubError {
  code?: string;
  description?: string;
  type?: number;
}

export interface HubResult<T = unknown> {
  isError?: boolean;
  firstError?: HubError;
  errors?: HubError[];
  value?: T;
}
