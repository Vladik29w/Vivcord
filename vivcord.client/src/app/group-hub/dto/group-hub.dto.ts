export interface GroupChatDTO {
  id: number;
  name: string;
  adminId: string;
  memberIds: string[];
}

export interface CreateGroupChatDTO {
  name: string;
}
