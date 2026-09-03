import { Component, ChangeDetectionStrategy, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ProfileService } from '../service/profile.service';
import { AccountService } from '@account/service/account.service';
import { UserProfileDTO } from '../dto/profile.dto';

@Component({
  selector: 'app-profile',
  imports: [FormsModule],
  templateUrl: './profile.html',
  styleUrl: './profile.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Profile implements OnInit {
  private readonly profileService = inject(ProfileService);
  public readonly accountService = inject(AccountService);
  private readonly destroyRef = inject(DestroyRef);

  public readonly profile = signal<UserProfileDTO | null>(null);
  public readonly isLoading = signal<boolean>(true);
  public readonly isEditingNickname = signal<boolean>(false);
  public readonly isSavingNickname = signal<boolean>(false);
  public readonly isUploadingAvatar = signal<boolean>(false);
  public readonly newDisplayName = signal<string>('');
  public readonly errorMessage = signal<string | null>(null);
  public readonly successMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadProfile();
  }

  public loadProfile(): void {
    const currentUser = this.accountService.currentUser();
    if (!currentUser?.id) {
      this.isLoading.set(false);
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.profileService.getUserProfile(currentUser.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (userProfile) => {
          this.profile.set(userProfile);
          this.newDisplayName.set(userProfile.displayName || currentUser.displayName || '');
          this.isLoading.set(false);
        },
        error: (err) => {
          console.error('[Profile] Failed to load user profile:', err);
          this.profile.set({
            userId: currentUser.id,
            displayName: currentUser.displayName,
            profilePictureUrl: null,
          });
          this.newDisplayName.set(currentUser.displayName);
          this.isLoading.set(false);
        },
      });
  }

  public startEditingNickname(): void {
    this.newDisplayName.set(this.profile()?.displayName || this.accountService.currentUser()?.displayName || '');
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.isEditingNickname.set(true);
  }

  public cancelEditingNickname(): void {
    this.isEditingNickname.set(false);
    this.errorMessage.set(null);
  }

  public saveDisplayName(): void {
    const trimmed = this.newDisplayName().trim();
    if (!trimmed) {
      this.errorMessage.set('Display name cannot be empty.');
      return;
    }

    const userId = this.profile()?.userId ?? this.accountService.currentUser()?.id;
    if (!userId) {
      this.errorMessage.set('User not found.');
      return;
    }

    this.isSavingNickname.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.profileService.changeDisplayName(userId, trimmed)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.profile.update(p => (p ? { ...p, displayName: trimmed } : null));
          this.accountService.currentUser.update(u => (u ? { ...u, displayName: trimmed } : null));
          this.isSavingNickname.set(false);
          this.isEditingNickname.set(false);
          this.successMessage.set('Display name updated successfully!');
        },
        error: (err) => {
          console.error('[Profile] Failed to update display name:', err);
          this.errorMessage.set('Failed to update display name.');
          this.isSavingNickname.set(false);
        },
      });
  }

  public async onAvatarFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    const userId = this.profile()?.userId ?? this.accountService.currentUser()?.id;
    if (!userId) {
      this.errorMessage.set('User not found.');
      return;
    }

    this.isUploadingAvatar.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    try {
      await this.profileService.uploadAvatar(userId, file);
      this.successMessage.set('Avatar updated successfully!');
      this.loadProfile();
    } catch (err) {
      console.error('[Profile] Failed to upload avatar:', err);
      this.errorMessage.set('Failed to upload avatar. Please try again.');
    } finally {
      this.isUploadingAvatar.set(false);
      input.value = '';
    }
  }
}
