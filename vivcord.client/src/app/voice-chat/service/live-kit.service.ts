import { Injectable, signal, computed } from '@angular/core';
import { Room, RoomEvent, RemoteTrack, Track, RoomOptions, Participant, LocalAudioTrack } from 'livekit-client';
import { VoiceParticipant } from '../dto/voice-chat.dto';
import { DenoiseTrackProcessor } from 'livekit-rnnoise-processor';

@Injectable({
  providedIn: 'root',
})
export class LiveKitService {
  private room: Room | null = null;
  readonly isConnected = signal(false);
  readonly isMuted = signal(false);
  readonly participants = signal<VoiceParticipant[]>([]);
  readonly error = signal<string | null>(null);
  readonly isNoiseFilterEnabled = signal(true);
  readonly participantCount = computed(() => this.participants().length);

  async connect(url: string, token: string): Promise<void> {
    this.error.set(null);

    const options: RoomOptions = {
      adaptiveStream: true,
      dynacast: true,
      audioCaptureDefaults: {
        echoCancellation: true,
        noiseSuppression: true,
        autoGainControl: true,
      }
    };

    this.room = new Room(options);
    this.registerEvents(this.room);
    try {
      await this.room.connect(url, token);
      await this.room.localParticipant.setMicrophoneEnabled(true);

      if (this.isNoiseFilterEnabled()) {
        const trackPub = this.room.localParticipant.getTrackPublication(Track.Source.Microphone);
        if (trackPub && trackPub.track) {
          const track = trackPub.track as LocalAudioTrack;
          const audioCtx = new AudioContext();
          if (audioCtx.state === 'suspended') {
            await audioCtx.resume();
          }
          track.setAudioContext(audioCtx);
          await track.setProcessor(new DenoiseTrackProcessor());
        }
      }

      const initialParticipants: VoiceParticipant[] = [];
      this.room.remoteParticipants.forEach(participant => {
        initialParticipants.push({ identity: participant.identity, name: participant.name, isSpeaking: false });
        participant.trackPublications.forEach(pub => {
          if (pub.isSubscribed && pub.track) {
            this.onTrackSubscribed(pub.track);
          }
        });
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
  async toggleNoiseFilter(): Promise<void> {
    if (!this.room || !this.room.localParticipant) {
      return;
    }
    try {
      const trackPub = this.room.localParticipant.getTrackPublication(Track.Source.Microphone);
      if (trackPub && trackPub.track) {
        const track = trackPub.track as LocalAudioTrack;
        const currentlyEnabled = this.isNoiseFilterEnabled();
        
        if (currentlyEnabled) {
          await track.stopProcessor();
          this.isNoiseFilterEnabled.set(false);
        } else {
          let audioCtx = (track as any).audioContext;
          if (!audioCtx) {
            audioCtx = new AudioContext();
          }
          if (audioCtx.state === 'suspended') {
            await audioCtx.resume();
          }
          track.setAudioContext(audioCtx);
          await track.setProcessor(new DenoiseTrackProcessor());
          this.isNoiseFilterEnabled.set(true);
        }
      }
    } catch (err) {
      this.error.set(`Failed to toggle noise filter: ${err}`);
      throw err;
    }
  }
  private registerEvents(room: Room): void {
    room
      .on(RoomEvent.TrackSubscribed, (track) => this.onTrackSubscribed(track))
      .on(RoomEvent.TrackUnsubscribed, (track) => this.onTrackUnsubscribed(track))
      .on(RoomEvent.ParticipantConnected, (p) => this.addParticipant(p.identity, p.name))
      .on(RoomEvent.ParticipantDisconnected, (p) => this.removeParticipant(p.identity))
      .on(RoomEvent.ActiveSpeakersChanged, (speakers) => this.updateSpeakers(speakers))
      .on(RoomEvent.Disconnected, () => this.onDisconnected());
  }
  private onTrackSubscribed(track: RemoteTrack): void {
    if (track.kind !== Track.Kind.Audio) return;
    const audioEl = track.attach();
    audioEl.autoplay = true;
    document.body.appendChild(audioEl);
  }
  private onTrackUnsubscribed(track: RemoteTrack): void {
    track.detach();
  }
  private onDisconnected(): void {
    this.isConnected.set(false);
    this.isMuted.set(false);
    this.participants.set([]);
  }
  private addParticipant(identity: string, name?: string): void {
    this.participants.update((list) => [
      ...list,
      { identity, name, isSpeaking: false },
    ]);
  }
  private removeParticipant(identity: string): void {
    this.participants.update((list) =>
      list.filter((p) => p.identity !== identity)
    );
  }
  private updateSpeakers(speakers: Participant[]): void {
    const activeSpeakerIds = new Set(speakers.map((s) => s.identity));
    this.participants.update((list) =>
      list.map((p) => ({ ...p, isSpeaking: activeSpeakerIds.has(p.identity) }))
    );
  }
}
