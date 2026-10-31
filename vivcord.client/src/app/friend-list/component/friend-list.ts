import { Component, inject, signal, computed, OnInit, DestroyRef, ChangeDetectionStrategy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FriendListService } from '../service/friend-list.service';
import { PendingFriendRequest } from '../dto/friend-list.dto';
import { GroupManagementService } from '../../group-hub/service/group-management.service';
import { GroupChatDTO } from '../../group-hub/dto/conversation.dto';
import { AccountService } from '@account/service/account.service';
import { LiveKitService } from '../../voice-chat/service/live-kit.service';
import { VoiceChatComponent } from '../../voice-chat/component/voice-chat/voice-chat';
import { ToastService } from '../../shared/toast/service/toast.service';
import { PrivateHubService } from '../../private-hub/service/private-hub.service';

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
  private readonly privateHubService = inject(PrivateHubService);
  private readonly groupManagement = inject(GroupManagementService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  public readonly accountService = inject(AccountService);
  public readonly livekitService = inject(LiveKitService);

  // Signals exposed directly from the services for reactive UI updates
  public readonly friendList = this.friendService.friendList;
  public readonly incomingRequests = this.friendService.incomingRequests;
  public readonly outgoingRequests = this.friendService.outgoingRequests;
  public readonly groupList = this.groupManagement.groupList;
  public readonly pendingGroupInvites = this.groupManagement.pendingGroupInvites;

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
    this.privateHubService.connectToHub();

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(event => {
        this.currentUrl.set(event.urlAfterRedirects || event.url);
      });

    this.friendService.refreshAll();
    this.groupManagement.refreshAll();

    // Real-time SignalR updates when friend requests are sent/accepted/declined/cancelled/removed
    this.privateHubService.friendsUpdated$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        const previousIncomingIds = new Set(this.incomingRequests().map(r => r.conversationId));
        const previousFriendIds = new Set(this.friendList().map(f => f.id));

        this.friendService
          .getFriendList()
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe(friends => {
            for (const f of friends) {
              if (!previousFriendIds.has(f.id) && f.conversationId) {
                this.privateHubService.joinConversation(f.conversationId);
              }
            }
          });

        this.friendService
          .getIncomingRequests()
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe(reqs => {
            for (const req of reqs) {
              if (!previousIncomingIds.has(req.conversationId)) {
                this.toastService.show({
                  title: 'New friend request',
                  message: `${req.displayName || req.userName} (@${req.userName}) sent you a friend request`,
                  avatarUrl: req.profilePictureUrl,
                  avatarInitials: this.getInitials(req.displayName || req.userName),
                  type: 'info',
                  onClick: () => this.activeTab.set('chats'),
                });
              }
            }
          });

        this.friendService
          .getOutgoingRequests()
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe();
      });

    // Real-time SignalR updates when group invites are sent/accepted/declined or membership changes
    this.privateHubService.groupsUpdated$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        const previousInviteIds = new Set(this.pendingGroupInvites().map(g => g.id));

        this.groupManagement
          .getMyGroups()
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe();

        this.groupManagement
          .getPendingInvites()
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe(invites => {
            for (const invite of invites) {
              if (!previousInviteIds.has(invite.id)) {
                this.toastService.show({
                  title: 'Group invitation',
                  message: `You were invited to "${invite.title || invite.name}"`,
                  type: 'info',
                  onClick: () => this.activeTab.set('groups'),
                });
              }
            }
          });
      });

    this.privateHubService.userStatusChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ userId, isOnline }) => {
        this.friendService.updateFriendOnlineStatus(userId, isOnline);
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
    this.friendService
      .sendFriendRequest(trimmed)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: conv => {
          this.showAddFriend.set(false);
          const allJoined =
            conv.members && conv.members.length > 0 && conv.members.every(m => m.status === 'Joined');

          if (allJoined) {
            this.privateHubService.joinConversation(conv.id);
            this.toastService.show({
              title: 'Friend added',
              message: `@${trimmed} has been added to your friend list`,
              type: 'success',
            });
          } else {
            this.toastService.show({
              title: 'Friend request sent',
              message: `Friend request sent to @${trimmed}`,
              type: 'info',
            });
          }
        },
        error: err => {
          console.error('failed to send friend request', err);
          this.toastService.show({
            title: 'Failed to add friend',
            message:
              err?.error?.detail ||
              err?.error?.title ||
              (typeof err?.error === 'string' ? err.error : null) ||
              'User not found or request already pending',
            type: 'error',
          });
        },
      });
  }

  acceptFriendRequest(req: PendingFriendRequest, event: Event): void {
    event.stopPropagation();
    this.friendService
      .acceptFriendRequest(req.conversationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.privateHubService.joinConversation(req.conversationId);
          this.toastService.show({
            title: 'Friend added',
            message: `You and ${req.displayName || req.userName} are now friends`,
            type: 'success',
          });
        },
        error: err => {
          console.error('failed to accept friend request', err);
          this.toastService.show({
            title: 'Error',
            message: 'Failed to accept friend request',
            type: 'error',
          });
        },
      });
  }

  declineFriendRequest(req: PendingFriendRequest, event: Event): void {
    event.stopPropagation();
    this.friendService
      .declineFriendRequest(req.conversationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toastService.show({
            title: 'Request declined',
            message: `Declined friend request from ${req.displayName || req.userName}`,
            type: 'info',
          });
        },
        error: err => console.error('failed to decline friend request', err),
      });
  }

  cancelFriendRequest(req: PendingFriendRequest, event: Event): void {
    event.stopPropagation();
    this.friendService
      .cancelFriendRequest(req.conversationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toastService.show({
            title: 'Request cancelled',
            message: `Cancelled friend request to ${req.displayName || req.userName}`,
            type: 'info',
          });
        },
        error: err => console.error('failed to cancel friend request', err),
      });
  }

  removeFriend(userName: string, event: Event): void {
    event.stopPropagation();
    this.friendService
      .removeFromFriendList(userName)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          if (this.isChatActive(userName)) {
            this.router.navigate(['/']);
          }
          this.toastService.show({
            title: 'Friend removed',
            message: `Removed ${userName} from friend list`,
            type: 'info',
          });
        },
        error: err => {
          console.error('failed to remove friend', err);
          this.toastService.show({
            title: 'Failed to remove friend',
            message: 'An error occurred while removing friend',
            type: 'error',
          });
        },
      });
  }

  createGroup(name: string): void {
    const trimmed = name.trim();
    if (!trimmed) return;
    this.groupManagement
      .createGroup(trimmed)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: group => {
          this.privateHubService.joinConversation(group.id);
          this.showCreateGroup.set(false);
          this.navigateToGroup(group.id);
          this.toastService.show({
            title: 'Group created',
            message: `Group "${group.title || group.name}" was created successfully`,
            type: 'success',
          });
        },
        error: err => {
          console.error('failed to create group', err);
          this.toastService.show({
            title: 'Failed to create group',
            message: 'An error occurred while creating group',
            type: 'error',
          });
        },
      });
  }

  acceptGroupInvite(group: GroupChatDTO, event: Event): void {
    event.stopPropagation();
    this.groupManagement
      .acceptInvite(group.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.privateHubService.joinConversation(group.id);
          this.navigateToGroup(group.id);
          this.toastService.show({
            title: 'Joined group',
            message: `You joined "${group.title || group.name}"`,
            type: 'success',
          });
        },
        error: err => {
          console.error('failed to accept group invite', err);
          this.toastService.show({
            title: 'Error',
            message: 'Failed to accept group invite',
            type: 'error',
          });
        },
      });
  }

  declineGroupInvite(group: GroupChatDTO, event: Event): void {
    event.stopPropagation();
    this.groupManagement
      .declineInvite(group.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toastService.show({
            title: 'Invite declined',
            message: `Declined invite to "${group.title || group.name}"`,
            type: 'info',
          });
        },
        error: err => console.error('failed to decline group invite', err),
      });
  }

  navigateToChat(userName: string): void {
    this.router.navigate(['/chat', userName]);
  }

  navigateToGroup(groupId: string): void {
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

  isGroupActive(groupId: string): boolean {
    return this.currentUrl().toLowerCase() === `/group/${String(groupId).toLowerCase()}`;
  }

  getInitials(name: string): string {
    return (name ? name.substring(0, 2) : '??').toUpperCase();
  }
}
