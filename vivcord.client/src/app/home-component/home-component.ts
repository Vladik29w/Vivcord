import {
  Component,
  OnInit,
  inject,
  ChangeDetectionStrategy,
  signal,
  ElementRef,
  DestroyRef,
  HostListener,
} from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { FriendListComponent } from '../friend-list/component/friend-list';

export const DEFAULT_SIDEBAR_WIDTH = 300;
export const MIN_SIDEBAR_WIDTH = 200;
export const MAX_SIDEBAR_WIDTH = 650;
export const SIDEBAR_WIDTH_STORAGE_KEY = 'vivcord_sidebar_width';

@Component({
  selector: 'app-home-component',
  standalone: true,
  imports: [RouterOutlet, FriendListComponent],
  templateUrl: './home-component.html',
  styleUrl: './home-component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomeComponent implements OnInit {
  private router = inject(Router);
  private elementRef = inject(ElementRef<HTMLElement>);
  private destroyRef = inject(DestroyRef);

  readonly sidebarWidth = signal<number>(this.loadInitialWidth());
  readonly isResizing = signal<boolean>(false);

  ngOnInit() {
    if (this.router.url === '/') {
      const lastChat = localStorage.getItem('lastChat');
      if (lastChat) {
        this.router.navigate(['/chat', lastChat]);
      }
    }

    this.destroyRef.onDestroy(() => {
      this.removeDragListeners();
    });
  }

  private loadInitialWidth(): number {
    if (typeof window === 'undefined' || !window.localStorage) {
      return DEFAULT_SIDEBAR_WIDTH;
    }
    try {
      const saved = localStorage.getItem(SIDEBAR_WIDTH_STORAGE_KEY);
      if (saved) {
        const parsed = parseInt(saved, 10);
        if (!isNaN(parsed) && parsed >= MIN_SIDEBAR_WIDTH && parsed <= MAX_SIDEBAR_WIDTH) {
          return parsed;
        }
      }
    } catch {
      // Fallback in case localStorage access is restricted
    }
    return DEFAULT_SIDEBAR_WIDTH;
  }

  startResize(event: MouseEvent): void {
    if (event.button !== 0) return;
    event.preventDefault();
    this.isResizing.set(true);

    window.addEventListener('mousemove', this.onMouseMove);
    window.addEventListener('mouseup', this.onMouseUp);
  }

  startResizeTouch(event: TouchEvent): void {
    if (event.touches.length !== 1) return;
    this.isResizing.set(true);

    window.addEventListener('touchmove', this.onTouchMove, { passive: false });
    window.addEventListener('touchend', this.onTouchEnd);
    window.addEventListener('touchcancel', this.onTouchEnd);
  }

  private onMouseMove = (event: MouseEvent): void => {
    if (!this.isResizing()) return;
    this.updateWidthFromPointer(event.clientX);
  };

  private onTouchMove = (event: TouchEvent): void => {
    if (!this.isResizing() || event.touches.length !== 1) return;
    event.preventDefault();
    this.updateWidthFromPointer(event.touches[0].clientX);
  };

  private onMouseUp = (): void => {
    this.stopResize();
  };

  private onTouchEnd = (): void => {
    this.stopResize();
  };

  private stopResize(): void {
    if (!this.isResizing()) return;
    this.isResizing.set(false);
    this.removeDragListeners();
    this.saveWidth(this.sidebarWidth());
  }

  private removeDragListeners(): void {
    if (typeof window === 'undefined') return;
    window.removeEventListener('mousemove', this.onMouseMove);
    window.removeEventListener('mouseup', this.onMouseUp);
    window.removeEventListener('touchmove', this.onTouchMove);
    window.removeEventListener('touchend', this.onTouchEnd);
    window.removeEventListener('touchcancel', this.onTouchEnd);
  }

  private updateWidthFromPointer(clientX: number): void {
    const hostEl = this.elementRef.nativeElement;
    const rect = hostEl.getBoundingClientRect();
    const newWidth = rect.right - clientX;
    this.applyWidth(newWidth, rect.width);
  }

  private applyWidth(newWidth: number, totalContainerWidth?: number): void {
    const containerWidth = totalContainerWidth ?? this.elementRef.nativeElement.getBoundingClientRect().width;
    // Maintain reasonable minimum width for the main chat area (at least 280px)
    const maxAllowedWidth = Math.min(
      MAX_SIDEBAR_WIDTH,
      Math.max(MIN_SIDEBAR_WIDTH, containerWidth - 280)
    );
    const clamped = Math.round(Math.max(MIN_SIDEBAR_WIDTH, Math.min(maxAllowedWidth, newWidth)));
    this.sidebarWidth.set(clamped);
  }

  private saveWidth(width: number): void {
    try {
      localStorage.setItem(SIDEBAR_WIDTH_STORAGE_KEY, width.toString());
    } catch {}
  }

  resetWidth(): void {
    this.sidebarWidth.set(DEFAULT_SIDEBAR_WIDTH);
    this.saveWidth(DEFAULT_SIDEBAR_WIDTH);
  }

  onResizerKeyDown(event: KeyboardEvent): void {
    const step = 20;
    if (event.key === 'ArrowLeft') {
      event.preventDefault();
      this.applyWidth(this.sidebarWidth() + step);
      this.saveWidth(this.sidebarWidth());
    } else if (event.key === 'ArrowRight') {
      event.preventDefault();
      this.applyWidth(this.sidebarWidth() - step);
      this.saveWidth(this.sidebarWidth());
    } else if (event.key === 'Home') {
      event.preventDefault();
      this.applyWidth(MIN_SIDEBAR_WIDTH);
      this.saveWidth(this.sidebarWidth());
    } else if (event.key === 'End') {
      event.preventDefault();
      this.applyWidth(MAX_SIDEBAR_WIDTH);
      this.saveWidth(this.sidebarWidth());
    } else if (event.key === 'Enter') {
      event.preventDefault();
      this.resetWidth();
    }
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    const rect = this.elementRef.nativeElement.getBoundingClientRect();
    if (rect.width > 0) {
      this.applyWidth(this.sidebarWidth(), rect.width);
    }
  }
}
