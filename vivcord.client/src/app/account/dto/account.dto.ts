export interface UserDTO {
  id: string;
  email: string;
  displayName: string;
  profilePictureUrl?: string | null;
  roles: string[];
}
export interface RegisterDTO {
  name: string
  email: string
  password: string
};
export interface LoginDTO {
  email: string
  password: string
}
