export interface UserProfileDTO {
  userId: string;
  userName?: string;
  displayName: string;
  profilePictureUrl: string | null;
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

