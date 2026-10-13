import { Component, inject, computed, effect, ElementRef, viewChild, ChangeDetectionStrategy, signal } from '@angular/core';
import { Router } from '@angular/router';
import { LiveKitService } from '../../service/live-kit.service';

@Component({
  selector: 'app-voice-chat-room',
  standalone: true,
  templateUrl: './voice-chat-room.html',
  styleUrl: './voice-chat-room.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VoiceChatRoomComponent {
  protected readonly livekitService = inject(LiveKitService);
  private readonly router = inject(Router);

  private readonly videoEl = viewChild<ElementRef<HTMLVideoElement>>('screenVideo');
  private readonly containerEl = viewChild<ElementRef<HTMLDivElement>>('videoContainer');
  private readonly sliderTrack = viewChild<ElementRef<HTMLDivElement>>('sliderTrack');

  protected readonly isSharing = computed(() => this.livekitService.isScreenSharing());
  protected readonly ownerName = computed(() => {
    const ownerId = this.livekitService.screenShareOwnerId();
    if (!ownerId) return null;
    return this.livekitService.participants().find(p => p.identity === ownerId)?.name
      ?? this.livekitService.screenShares().find(s => s.identity === ownerId)?.name
      ?? ownerId;
  });
  protected readonly hasVideo = computed(() =>
    this.isSharing() || this.livekitService.screenShareTrack() !== null);
  protected readonly showAudioControls = computed(() =>
    !this.isSharing() && (this.hasVideo() || this.livekitService.hasScreenShareAudio()));

  protected readonly effectiveVolume = computed(() => {
    if (this.livekitService.isScreenShareAudioMuted()) return 0;
    return this.livekitService.screenShareAudioVolume();
  });

  protected readonly displayPercent = signal<number>(100);
  protected readonly isSnapping = signal<boolean>(false);
  protected readonly fillPercent = computed(() =>
    Math.max(0, Math.min(100, this.displayPercent())));

  private isDragging = false;
  private snapTimeout: ReturnType<typeof setTimeout> | null = null;

  onSliderPointerDown(event: PointerEvent): void {
    const track = this.sliderTrack()?.nativeElement;
    if (!track) return;
    if (this.snapTimeout) {
      clearTimeout(this.snapTimeout);
      this.snapTimeout = null;
    }
    try {
      track.setPointerCapture(event.pointerId);
    } catch {}
    this.isDragging = true;
    this.isSnapping.set(false);
    this.updateFromPointer(event);
  }

  onSliderPointerMove(event: PointerEvent): void {
    if (!this.isDragging) return;
    this.updateFromPointer(event);
  }

  onSliderPointerUp(event: PointerEvent): void {
    if (!this.isDragging) return;
    this.isDragging = false;
    const track = this.sliderTrack()?.nativeElement;
    if (track) {
      try {
        track.releasePointerCapture(event.pointerId);
      } catch {}
    }

    // On release it removes the overshoot and lets .snap spring home
    const clamped = Math.max(0, Math.min(100, this.effectiveVolume()));
    this.isSnapping.set(true);
    this.displayPercent.set(clamped);

    if (this.snapTimeout) clearTimeout(this.snapTimeout);
    this.snapTimeout = setTimeout(() => {
      this.isSnapping.set(false);
      this.snapTimeout = null;
    }, 500);
  }

  private updateFromPointer(event: PointerEvent): void {
    const track = this.sliderTrack()?.nativeElement;
    if (!track) return;
    const rect = track.getBoundingClientRect();
    if (rect.width === 0) return;

    //animation for sound slider
    const raw = ((event.clientX - rect.left) / rect.width) * 100;
    const clamped = Math.max(0, Math.min(100, raw));
    const pct = clamped + (raw - clamped) * 0.32;

    this.displayPercent.set(pct);

    const vol = Math.round(clamped);
    if (this.livekitService.isScreenShareAudioMuted() && vol > 0) {
      this.livekitService.toggleScreenShareAudioMute();
    }
    this.livekitService.setScreenShareAudioVolume(vol);
  }

  onSliderKeyDown(event: KeyboardEvent): void {
    let step = 0;
    if (event.key === 'ArrowLeft' || event.key === 'ArrowDown') step = -5;
    else if (event.key === 'ArrowRight' || event.key === 'ArrowUp') step = 5;
    if (step !== 0) {
      event.preventDefault();
      const next = Math.max(0, Math.min(100, this.effectiveVolume() + step));
      this.isSnapping.set(true);
      this.displayPercent.set(next);
      if (this.livekitService.isScreenShareAudioMuted() && next > 0) {
        this.livekitService.toggleScreenShareAudioMute();
      }
      this.livekitService.setScreenShareAudioVolume(next);
      if (this.snapTimeout) clearTimeout(this.snapTimeout);
      this.snapTimeout = setTimeout(() => {
        this.isSnapping.set(false);
        this.snapTimeout = null;
      }, 500);
    }
  }

  toggleAudioMute(): void {
    this.isSnapping.set(true);
    this.livekitService.toggleScreenShareAudioMute();
    const target = this.livekitService.isScreenShareAudioMuted() ? 0 : this.livekitService.screenShareAudioVolume();
    this.displayPercent.set(target);
    if (this.snapTimeout) clearTimeout(this.snapTimeout);
    this.snapTimeout = setTimeout(() => {
      this.isSnapping.set(false);
      this.snapTimeout = null;
    }, 500);
  }

  constructor() {
    effect(() => {
      const track = this.livekitService.screenShareTrack();
      const localTrack = this.livekitService.localScreenShareTrack();
      const el = this.videoEl()?.nativeElement;
      if (!el) return;
      const activeTrack = track ?? localTrack;
      if (activeTrack) {
        activeTrack.attach(el);
      } else {
        el.srcObject = null;
      }
    });

    effect(() => {
      const vol = this.effectiveVolume();
      if (!this.isDragging) {
        this.displayPercent.set(vol);
      }
    });
  }

  async toggleFullscreen(): Promise<void> {
    const container = this.containerEl()?.nativeElement;
    if (!container) return;
    if (document.fullscreenElement) {
      await document.exitFullscreen();
    } else {
      await container.requestFullscreen();
    }
  }

  close(): void {
    const lastChat = localStorage.getItem('lastChat');
    if (lastChat) {
      this.router.navigate(['/chat', lastChat]);
    } else {
      this.router.navigate(['/']);
    }
  }

  async stopSharing(): Promise<void> {
    await this.livekitService.stopScreenShare();
  }
}
