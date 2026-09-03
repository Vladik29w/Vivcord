export interface UserProfileDTO {
  userId: string;
  displayName: string;
  profilePictureUrl: string | null;
}

export interface UploadTokenResponse {
  uploadUrl: string;
  blobName: string;
}

export interface ChangeDisplayNameRequest {
  userId: string;
  displayName: string;
}

export interface UpdateProfilePictureRequest {
  userId: string;
  blobName: string;
}
