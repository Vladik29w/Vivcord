import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FriendListService } from '../service/friend-list.service';
import { Friend } from '../dto/friend-list.dto';
import { GroupManagementService } from '../../group-hub/service/group-management.service';
import { GroupChatDTO } from '../../group-hub/dto/group-hub.dto';

@Component({
  selector: 'app-friend-list',
  standalone: true,
  templateUrl: './friend-list.html',
  styleUrl: './friend-list.css'
})
export class FriendListComponent implements OnInit {
  private friendService = inject(FriendListService);
  private groupManagement = inject(GroupManagementService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  public friendList = signal<Friend[]>([]);
  public groupList = signal<GroupChatDTO[]>([]);

  ngOnInit(): void {
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

  addFriend(userName: string): void {
    this.friendService.addFriend(userName)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (newFriend) => {
          this.friendList.update(list => [...list, newFriend]);
        },
        error: (err) => console.error('failed to add friend', err)
      });
  }

  removeFriend(userName: string): void {
    this.friendService.removeFromFriendList(userName)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.friendList.update(list => list.filter(f => f.userName !== userName));
        },
        error: (err) => console.error('failed to remove friend', err)
      });
  }

  createGroup(name: string): void {
    if (!name.trim()) return;
    this.groupManagement.createGroup(name)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (group) => {
          this.groupList.update(list => [...list, group]);
          this.navigateToGroup(group.id);
        },
        error: (err) => console.error('failed to create group', err)
      });
  }

  navigateToChat(userName: string): void {
    this.router.navigate(['/chat', userName]);
  }

  navigateToGroup(groupId: number): void {
    this.router.navigate(['/group', groupId]);
  }
}
