import { Component, inject, signal, computed, OnInit, DestroyRef, ChangeDetectionStrategy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FriendListService } from '../service/friend-list.service';
import { Friend } from '../dto/friend-list.dto';
import { GroupManagementService } from '../../group-hub/service/group-management.service';
import { GroupChatDTO } from '../../group-hub/dto/group-hub.dto';
import { AccountService } from '@account/service/account.service';
import { LiveKitService } from '../../voice-chat/service/live-kit.service';
import { VoiceChatComponent } from '../../voice-chat/component/voice-chat/voice-chat';
import { ToastService } from '../../shared/toast/service/toast.service';

@Component({
  selector: 'app-friend-list',
  standalone: true,
  imports: [VoiceChatComponent],
  templateUrl: './friend-list.html',
  styleUrl: './friend-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FriendListComponent implements OnInit {
  private readonly friendService = inject(FriendListService);
  private readonly groupManagement = inject(GroupManagementService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  public readonly accountService = inject(AccountService);
  public readonly livekitService = inject(LiveKitService);

  public readonly friendList = signal<Friend[]>([]);
  public readonly groupList = signal<GroupChatDTO[]>([]);
  public readonly activeTab = signal<'chats' | 'groups'>('chats');
  public readonly showAddFriend = signal<boolean>(false);
  public readonly showCreateGroup = signal<boolean>(false);
  public readonly currentUrl = signal<string>(this.router.url);

  public readonly userDisplayName = computed(() => {
    const user = this.accountService.currentUser();
    return user?.displayName || (user?.email ? user.email.split('@')[0] : 'yourname');
  });

  public readonly userAvatarInitials = computed(() => {
    const name = this.userDisplayName();
    return (name ? name.substring(0, 2) : 'ME').toUpperCase();
  });

  public readonly userProfilePictureUrl = computed(() => this.accountService.currentUser()?.profilePictureUrl ?? null);

  ngOnInit(): void {
    this.router.events
      .pipe(
        filter(event => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((event: any) => {
        this.currentUrl.set(event.urlAfterRedirects || event.url);
      });

    this.friendService.getFriendList()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (friends) => this.friendList.set(friends),
        error: (err) => console.error('failed to load friends', err)
      });

    this.groupManagement.getMyGroups()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (groups) => this.groupList.set(groups),
        error: (err) => console.error('failed to load groups', err)
      });
  }

  toggleAddFriend(): void {
    this.showAddFriend.update(v => !v);
    if (this.showAddFriend()) this.showCreateGroup.set(false);
  }

  toggleCreateGroup(): void {
    this.showCreateGroup.update(v => !v);
    if (this.showCreateGroup()) this.showAddFriend.set(false);
  }

  addFriend(userName: string): void {
    const trimmed = userName.trim();
    if (!trimmed) return;
    this.friendService.addFriend(trimmed)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (newFriend) => {
          this.friendList.update(list => [...list, newFriend]);
          this.showAddFriend.set(false);
          this.toastService.show({
            title: 'Friend added',
            message: `${newFriend.userName} has been added to your friend list`,
            type: 'success',
          });
        },
        error: (err) => {
          console.error('failed to add friend', err);
          this.toastService.show({
            title: 'Failed to add friend',
            message: err?.error?.detail || err?.error || 'User not found or mutual request pending',
            type: 'error',
          });
        }
      });
  }

  removeFriend(userName: string, event: Event): void {
    event.stopPropagation();
    this.friendService.removeFromFriendList(userName)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.friendList.update(list => list.filter(f => f.userName !== userName));
          this.toastService.show({
            title: 'Friend removed',
            message: `Removed ${userName} from friend list`,
            type: 'info',
          });
        },
        error: (err) => {
          console.error('failed to remove friend', err);
          this.toastService.show({
            title: 'Failed to remove friend',
            message: 'An error occurred while removing friend',
            type: 'error',
          });
        }
      });
  }

  createGroup(name: string): void {
    const trimmed = name.trim();
    if (!trimmed) return;
    this.groupManagement.createGroup(trimmed)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (group) => {
          this.groupList.update(list => [...list, group]);
          this.showCreateGroup.set(false);
          this.navigateToGroup(group.id);
          this.toastService.show({
            title: 'Group created',
            message: `Group "${group.name}" was created successfully`,
            type: 'success',
          });
        },
        error: (err) => {
          console.error('failed to create group', err);
          this.toastService.show({
            title: 'Failed to create group',
            message: 'An error occurred while creating group',
            type: 'error',
          });
        }
      });
  }

  navigateToChat(userName: string): void {
    this.router.navigate(['/chat', userName]);
  }

  navigateToGroup(groupId: number): void {
    this.router.navigate(['/group', groupId]);
  }

  navigateToProfile(): void {
    if (this.router.url.includes('/profile')) {
      const lastChat = localStorage.getItem('lastChat');
      if (lastChat) {
        this.router.navigate(['/chat', lastChat]);
      } else {
        this.router.navigate(['/']);
      }
    } else {
      this.router.navigate(['/profile']);
    }
  }

  isChatActive(userName: string): boolean {
    return this.currentUrl().toLowerCase() === `/chat/${userName.toLowerCase()}`;
  }

  isGroupActive(groupId: number): boolean {
    return this.currentUrl().toLowerCase() === `/group/${groupId}`;
  }

  getInitials(name: string): string {
    return (name ? name.substring(0, 2) : '??').toUpperCase();
  }
}
