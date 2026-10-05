export interface UserProfileDTO {
  id?: string;
  userId?: string;
  userName: string;
  displayName: string;
  profilePictureUrl: string | null;
  isOnline?: boolean;
}

export interface UploadTokenResponse {
  uploadUrl: string;
  blobName: string;
}

export interface ChangeDisplayNameRequest {
  displayName: string;
}

export interface UpdateProfilePictureRequest {
  blobName: string;
}
