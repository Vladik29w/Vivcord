import { Injectable, signal, computed } from '@angular/core';
import { VoiceParticipant } from '../dto/voice-chat.dto';

// Type-only imports — erased at compile time, zero runtime cost
import type { Room, RemoteTrack, RoomOptions, Participant, LocalAudioTrack, RemoteVideoTrack, LocalVideoTrack, ScreenShareCaptureOptions, TrackPublishOptions, RemoteTrackPublication } from 'livekit-client';

// Cached dynamic imports so the SDK is fetched only once
let livekitModule: typeof import('livekit-client') | null = null;
let rnnoiseModule: typeof import('livekit-rnnoise-processor') | null = null;

async function getLivekit() {
  if (!livekitModule) {
    livekitModule = await import('livekit-client');
  }
  return livekitModule;
}

async function getRnnoise() {
  if (!rnnoiseModule) {
    rnnoiseModule = await import('livekit-rnnoise-processor');
  }
  return rnnoiseModule;
}

@Injectable({
  providedIn: 'root',
})
export class LiveKitService {
  private room: Room | null = null;
  private readonly attachedAudioElements = new Set<HTMLAudioElement>();
  private screenShareAudioElement: HTMLAudioElement | null = null;

  readonly isConnected = signal(false);
  readonly isMuted = signal(false);
  readonly isDeafened = signal(false);
  readonly isLocalSpeaking = signal(false);
  readonly currentRoomId = signal<string | null>(null);
  readonly activeCallTitle = signal<string | null>(null);
  readonly participants = signal<VoiceParticipant[]>([]);
  readonly error = signal<string | null>(null);
  readonly isNoiseFilterEnabled = signal(true);
  readonly participantCount = computed(() => this.participants().length);
  readonly isScreenSharing = signal(false);
  readonly screenShareResolution = signal<'720p' | '1080p'>('1080p');
  readonly screenShareFrameRate = signal<30 | 60>(30);
  readonly screenShares = signal<{ identity: string; name: string }[]>([]);
  readonly screenShareTrack = signal<RemoteVideoTrack | null>(null);
  readonly screenShareOwnerId = signal<string | null>(null);
  readonly localScreenShareTrack = signal<LocalVideoTrack | null>(null);
  readonly hasScreenShareAudio = signal<boolean>(false);
  readonly screenShareAudioVolume = signal<number>(100);
  readonly isScreenShareAudioMuted = signal<boolean>(false);

  async connect(url: string, token: string, roomId?: string, title?: string): Promise<void> {
    this.error.set(null);
    if (roomId) this.currentRoomId.set(roomId);
    if (title) this.activeCallTitle.set(title);

    const lk = await getLivekit();

    const options: RoomOptions = {
      adaptiveStream: true,
      dynacast: true,
      audioCaptureDefaults: {
        echoCancellation: true,
        noiseSuppression: true,
        autoGainControl: true,
      }
    };

    this.room = new lk.Room(options);
    this.registerEvents(this.room);
    try {
      await this.room.connect(url, token);
      await this.room.localParticipant.setMicrophoneEnabled(true);

      if (this.isNoiseFilterEnabled()) {
        const trackPub = this.room.localParticipant.getTrackPublication(lk.Track.Source.Microphone);
        if (trackPub && trackPub.track) {
          const track = trackPub.track as LocalAudioTrack;
          const audioCtx = new AudioContext();
          if (audioCtx.state === 'suspended') {
            await audioCtx.resume();
          }
          track.setAudioContext(audioCtx);
          const rnnoise = await getRnnoise();
          await track.setProcessor(new rnnoise.DenoiseTrackProcessor());
        }
      }

      const initialParticipants: VoiceParticipant[] = [];
      this.room.remoteParticipants.forEach(participant => {
        initialParticipants.push({
          identity: participant.identity,
          name: participant.name,
          avatarUrl: participant.metadata || null,
          isSpeaking: false
        });
        participant.trackPublications.forEach(pub => {
          if (pub.isSubscribed && pub.track) {
            this.onTrackSubscribed(pub.track as RemoteTrack, participant, pub as RemoteTrackPublication);
          }
        });
        // Check for existing screen share
        const lk = livekitModule;
        if (lk) {
          const screenPub = participant.getTrackPublication(lk.Track.Source.ScreenShare);
          if (screenPub?.isSubscribed) {
            this.addScreenShare(participant.identity, participant.name || participant.identity);
            if (screenPub.track && !this.screenShareTrack()) {
              this.screenShareTrack.set(screenPub.track as RemoteVideoTrack);
              this.screenShareOwnerId.set(participant.identity);
            }
          }
          const screenAudioPub = participant.getTrackPublication(lk.Track.Source.ScreenShareAudio);
          if (screenAudioPub?.isSubscribed && screenAudioPub.track && !this.screenShareAudioElement) {
            this.onTrackSubscribed(screenAudioPub.track as RemoteTrack, participant, screenAudioPub as RemoteTrackPublication);
          }
        }
      });
      this.participants.update((list) => {
        const existingIdentities = new Set(list.map(p => p.identity));
        const newParticipants = initialParticipants.filter(p => !existingIdentities.has(p.identity));
        return [...list, ...newParticipants];
      });
      this.isConnected.set(true);
    }
    catch (err) {
      this.error.set(`Connection failed: ${err}`);
      this.currentRoomId.set(null);
      this.activeCallTitle.set(null);
      throw err;
    }
  }

  async disconnect(): Promise<void> {
    if (this.room) {
      try {
        await this.room.disconnect();
      } catch (err) {
        this.error.set(`Disconnection failed: ${err}`);
        throw err;
      } finally {
        this.room = null;
        this.cleanupAudioElements();
        this.onDisconnected();
      }
    }
  }

  async toggleMic(): Promise<void> {
    if (!this.room || !this.room.localParticipant) {
      return;
    }
    try {
      const currentlyMuted = this.isMuted();
      await this.room.localParticipant.setMicrophoneEnabled(currentlyMuted);
      this.isMuted.set(!currentlyMuted);
    } catch (err) {
      this.error.set(`Failed to toggle microphone: ${err}`);
      throw err;
    }
  }

  toggleDeafen(): void {
    const nextState = !this.isDeafened();
    this.isDeafened.set(nextState);
    this.attachedAudioElements.forEach(el => {
      if (el === this.screenShareAudioElement) {
        el.muted = nextState || this.isScreenShareAudioMuted();
      } else {
        el.muted = nextState;
      }
    });
  }

  async toggleNoiseFilter(): Promise<void> {
    if (!this.room || !this.room.localParticipant) {
      return;
    }
    try {
      const lk = await getLivekit();
      const trackPub = this.room.localParticipant.getTrackPublication(lk.Track.Source.Microphone);
      if (trackPub && trackPub.track) {
        const track = trackPub.track as LocalAudioTrack;
        const currentlyEnabled = this.isNoiseFilterEnabled();

        if (currentlyEnabled) {
          await track.stopProcessor();
          this.isNoiseFilterEnabled.set(false);
        } else {
          let audioCtx = (track as LocalAudioTrack & { audioContext?: AudioContext }).audioContext;
          if (!audioCtx) {
            audioCtx = new AudioContext();
          }
          if (audioCtx.state === 'suspended') {
            await audioCtx.resume();
          }
          track.setAudioContext(audioCtx);
          const rnnoise = await getRnnoise();
          await track.setProcessor(new rnnoise.DenoiseTrackProcessor());
          this.isNoiseFilterEnabled.set(true);
        }
      }
    } catch (err) {
      this.error.set(`Failed to toggle noise filter: ${err}`);
      throw err;
    }
  }

  async startScreenShare(
    resolution: '720p' | '1080p' = '1080p',
    frameRate: 30 | 60 = 30,
    includeAudio: boolean = true
  ): Promise<void> {
    if (!this.room || !this.room.localParticipant) return;
    try {
      const { width, height } = resolution === '720p'
        ? { width: 1280, height: 720 }
        : { width: 1920, height: 1080 };
      const options: ScreenShareCaptureOptions = {
        resolution: { width, height, frameRate },
        audio: includeAudio ? {
          autoGainControl: false,
          echoCancellation: false,
          noiseSuppression: false,
        } : false,
        systemAudio: includeAudio ? 'include' : 'exclude',
      };
      const publishOptions: TrackPublishOptions = {
        screenShareEncoding: {
          maxBitrate: resolution === '720p' ? 2_000_000 : 4_500_000,
          maxFramerate: frameRate,
        },
        audioPreset: { maxBitrate: 128_000 },
        dtx: true,
        simulcast: true,
      };
      await this.room.localParticipant.setScreenShareEnabled(true, options, publishOptions);
      this.screenShareResolution.set(resolution);
      this.screenShareFrameRate.set(frameRate);
      this.isScreenSharing.set(true);
      const lk = await getLivekit();
      const pub = this.room.localParticipant.getTrackPublication(lk.Track.Source.ScreenShare);
      if (pub?.track) {
        this.localScreenShareTrack.set(pub.track as LocalVideoTrack);
      }
    } catch (err) {
      this.error.set(`Screen share failed: ${err}`);
      this.isScreenSharing.set(false);
      this.localScreenShareTrack.set(null);
      throw err;
    }
  }

  async stopScreenShare(): Promise<void> {
    if (!this.room || !this.room.localParticipant) return;
    try {
      await this.room.localParticipant.setScreenShareEnabled(false);
      this.isScreenSharing.set(false);
      this.localScreenShareTrack.set(null);
      this.hasScreenShareAudio.set(false);
    } catch (err) {
      this.error.set(`Screen share stop failed: ${err}`);
      throw err;
    }
  }

  async toggleScreenShare(): Promise<void> {
    if (this.isScreenSharing()) {
      await this.stopScreenShare();
    } else {
      await this.startScreenShare(this.screenShareResolution(), this.screenShareFrameRate());
    }
  }

  setScreenShareQuality(_resolution: '720p' | '1080p', _frameRate: 30 | 60): void {
    // No-op: quality is selected at launch time
  }

  setScreenShareAudioVolume(volume: number): void {
    const clamped = Math.max(0, Math.min(100, Math.round(volume)));
    this.screenShareAudioVolume.set(clamped);
    if (this.screenShareAudioElement) {
      this.screenShareAudioElement.volume = this.isScreenShareAudioMuted() ? 0 : clamped / 100;
    }
  }

  toggleScreenShareAudioMute(): void {
    const nextMuted = !this.isScreenShareAudioMuted();
    this.isScreenShareAudioMuted.set(nextMuted);
    if (this.screenShareAudioElement) {
      this.screenShareAudioElement.muted = this.isDeafened() || nextMuted;
      this.screenShareAudioElement.volume = nextMuted ? 0 : this.screenShareAudioVolume() / 100;
    }
  }

  private cleanupScreenShareAudio(): void {
    if (this.screenShareAudioElement) {
      this.attachedAudioElements.delete(this.screenShareAudioElement);
      this.screenShareAudioElement.remove();
      this.screenShareAudioElement = null;
    }
    this.hasScreenShareAudio.set(false);
  }

  private async registerEvents(room: Room): Promise<void> {
    const lk = await getLivekit();
    room
      .on(lk.RoomEvent.TrackSubscribed, (track, pub, p) => this.onTrackSubscribed(track as RemoteTrack, p as Participant, pub as RemoteTrackPublication))
      .on(lk.RoomEvent.TrackUnsubscribed, (track, pub) => this.onTrackUnsubscribed(track as RemoteTrack, pub as RemoteTrackPublication))
      .on(lk.RoomEvent.ParticipantConnected, (p) => this.addParticipant(p.identity, p.name, p.metadata))
      .on(lk.RoomEvent.ParticipantDisconnected, (p) => this.removeParticipant(p.identity))
      .on(lk.RoomEvent.ParticipantMetadataChanged, (_metadata, p) => {
        if (p) this.updateParticipantMetadata(p as Participant);
      })
      .on(lk.RoomEvent.ActiveSpeakersChanged, (speakers) => this.updateSpeakers(speakers as Participant[]))
      .on(lk.RoomEvent.TrackPublished, (pub, p) => {
        if (pub.source === lk.Track.Source.ScreenShare) {
          this.addScreenShare(p.identity, p.name || p.identity);
        }
      })
      .on(lk.RoomEvent.TrackUnpublished, (pub, p) => {
        if (pub.source === lk.Track.Source.ScreenShare) {
          this.removeScreenShare(p.identity);
        }
        if (pub.source === lk.Track.Source.ScreenShareAudio) {
          this.cleanupScreenShareAudio();
        }
      })
      .on(lk.RoomEvent.LocalTrackPublished, (pub) => {
        if (pub.source === lk.Track.Source.ScreenShare) {
          const lp = this.room?.localParticipant;
          if (lp) this.addScreenShare(lp.identity, lp.name || lp.identity);
        }
      })
      .on(lk.RoomEvent.LocalTrackUnpublished, (pub) => {
        if (pub.source === lk.Track.Source.ScreenShare) {
          const lp = this.room?.localParticipant;
          if (lp) this.removeScreenShare(lp.identity);
          this.isScreenSharing.set(false);
          this.localScreenShareTrack.set(null);
        }
      })
      .on(lk.RoomEvent.Disconnected, () => this.onDisconnected());
  }

  private onTrackSubscribed(track: RemoteTrack, participant?: Participant, pub?: RemoteTrackPublication): void {
    const lk = livekitModule;
    const isScreenAudio =
      track.source === lk?.Track.Source.ScreenShareAudio ||
      pub?.source === lk?.Track.Source.ScreenShareAudio ||
      (track.source as string) === 'screen_share_audio' ||
      (pub?.source as string) === 'screen_share_audio';

    if (isScreenAudio) {
      this.cleanupScreenShareAudio();
      const audioEl = track.attach();
      audioEl.autoplay = true;
      audioEl.volume = this.isScreenShareAudioMuted() ? 0 : this.screenShareAudioVolume() / 100;
      audioEl.muted = this.isDeafened() || this.isScreenShareAudioMuted();
      this.screenShareAudioElement = audioEl;
      this.attachedAudioElements.add(audioEl);
      document.body.appendChild(audioEl);
      this.hasScreenShareAudio.set(true);
      return;
    }

    if (track.kind === 'video') {
      this.screenShareTrack.set(track as RemoteVideoTrack);
      this.screenShareOwnerId.set(participant?.identity ?? null);
      return;
    }

    if (track.kind !== 'audio') return;
    const audioEl = track.attach();
    audioEl.autoplay = true;
    audioEl.muted = this.isDeafened();
    this.attachedAudioElements.add(audioEl);
    document.body.appendChild(audioEl);
  }

  private onTrackUnsubscribed(track: RemoteTrack, pub?: RemoteTrackPublication): void {
    const lk = livekitModule;
    const isScreenAudio =
      track.source === lk?.Track.Source.ScreenShareAudio ||
      pub?.source === lk?.Track.Source.ScreenShareAudio ||
      (track.source as string) === 'screen_share_audio' ||
      (pub?.source as string) === 'screen_share_audio';

    if (isScreenAudio) {
      this.cleanupScreenShareAudio();
    }
    if (track.kind === 'video') {
      this.screenShareTrack.set(null);
      this.screenShareOwnerId.set(null);
    }
    const detached = track.detach();
    detached.forEach(el => {
      this.attachedAudioElements.delete(el);
      el.remove();
    });
  }

  private cleanupAudioElements(): void {
    this.attachedAudioElements.forEach(el => {
      el.remove();
    });
    this.attachedAudioElements.clear();
    this.screenShareAudioElement = null;
    this.hasScreenShareAudio.set(false);
  }

  private onDisconnected(): void {
    this.isConnected.set(false);
    this.isMuted.set(false);
    this.isDeafened.set(false);
    this.isLocalSpeaking.set(false);
    this.currentRoomId.set(null);
    this.activeCallTitle.set(null);
    this.participants.set([]);
    this.isScreenSharing.set(false);
    this.screenShares.set([]);
    this.screenShareTrack.set(null);
    this.screenShareOwnerId.set(null);
    this.localScreenShareTrack.set(null);
    this.hasScreenShareAudio.set(false);
    this.screenShareAudioElement = null;
  }

  private addParticipant(identity: string, name?: string, metadata?: string): void {
    this.participants.update((list) => {
      const existing = list.find((p) => p.identity === identity);
      if (existing) {
        return list.map((p) =>
          p.identity === identity
            ? { ...p, name: name ?? p.name, avatarUrl: metadata || p.avatarUrl }
            : p
        );
      }
      return [
        ...list,
        { identity, name, avatarUrl: metadata || null, isSpeaking: false },
      ];
    });
  }

  private updateParticipantMetadata(participant: Participant): void {
    this.participants.update((list) =>
      list.map((p) =>
        p.identity === participant.identity
          ? { ...p, avatarUrl: participant.metadata || null, name: participant.name ?? p.name }
          : p
      )
    );
  }

  private removeParticipant(identity: string): void {
    this.participants.update((list) =>
      list.filter((p) => p.identity !== identity)
    );
  }

  private addScreenShare(identity: string, name: string): void {
    this.screenShares.update(list => {
      if (list.some(s => s.identity === identity)) return list;
      return [...list, { identity, name }];
    });
  }

  private removeScreenShare(identity: string): void {
    this.screenShares.update(list => list.filter(s => s.identity !== identity));
  }

  private updateSpeakers(speakers: Participant[]): void {
    const activeSpeakerIds = new Set(speakers.map((s) => s.identity));
    const localId = this.room?.localParticipant?.identity;
    this.isLocalSpeaking.set(localId ? activeSpeakerIds.has(localId) : false);

    this.participants.update((list) =>
      list.map((p) => ({ ...p, isSpeaking: activeSpeakerIds.has(p.identity) }))
    );
  }
}

