import { Injectable, signal } from '@angular/core';
import { Toast } from '../dto/toast.dto';

@Injectable({
  providedIn: 'root',
})
export class ToastService {
  readonly toasts = signal<Toast[]>([]);

  show(options: {
    title?: string;
    message: string;
    type?: 'info' | 'success' | 'warning' | 'error' | 'message';
    avatarUrl?: string | null;
    avatarInitials?: string;
    onClick?: () => void;
    duration?: number;
  }): string {
    const id = crypto.randomUUID();
    const duration = options.duration ?? 4200;

    const newToast: Toast = {
      id,
      title: options.title,
      message: options.message,
      type: options.type ?? 'info',
      avatarUrl: options.avatarUrl,
      avatarInitials: options.avatarInitials,
      visible: false,
      onClick: options.onClick,
    };

    // Add toast to list
    this.toasts.update((toasts) => [...toasts, newToast]);

    // Animate in (.show) on next microtask/frame
    setTimeout(() => {
      this.toasts.update((toasts) =>
        toasts.map((t) => (t.id === id ? { ...t, visible: true } : t))
      );
    }, 20);

    // Auto dismiss after ~2.2s
    setTimeout(() => {
      this.dismiss(id);
    }, duration);

    return id;
  }

  dismiss(id: string): void {
    // Remove .show class first so transition plays out
    this.toasts.update((toasts) =>
      toasts.map((t) => (t.id === id ? { ...t, visible: false } : t))
    );

    // Remove element completely after animation completes (550ms)
    setTimeout(() => {
      this.toasts.update((toasts) => toasts.filter((t) => t.id !== id));
    }, 600);
  }
}
