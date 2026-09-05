export interface Toast {
  id: string;
  title?: string;
  message: string;
  type?: 'info' | 'success' | 'warning' | 'error' | 'message';
  avatarUrl?: string | null;
  avatarInitials?: string;
  visible: boolean;
  onClick?: () => void;
}
