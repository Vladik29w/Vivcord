import { UserProfileDTO } from '../../profile/dto/profile.dto';

export type { UserProfileDTO };

export type ConversationType = 'Direct' | 'Group';
export type MemberRole = 'Admin' | 'Member';
export type MemberStatus = 'Pending' | 'Joined' | 'Left';

export interface ConversationMemberDTO {
  userId: string;
  userName: string;
  displayName: string;
  profilePictureUrl?: string | null;
  role: MemberRole;
  status: MemberStatus;
  invitedById?: string | null;
}

export interface ConversationDTO {
  id: string;
  type: ConversationType;
  title?: string | null;
  name?: string;
  avatarUrl?: string | null;
  createdAt: string;
  lastMessageAt?: string | null;
  members: ConversationMemberDTO[];
  adminId?: string;
}

export interface CreateGroupConversationDTO {
  name: string;
}

export interface InviteToGroupRequest {
  username: string;
}

export interface AssignAdminRequest {
  newAdminUsername: string;
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

export type GroupChatDTO = ConversationDTO;
export type CreateGroupChatDTO = CreateGroupConversationDTO;
export type CreateConversationDTO = CreateGroupConversationDTO;
