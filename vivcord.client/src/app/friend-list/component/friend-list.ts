import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FriendListService } from '../service/friend-list.service';
import { Friend } from '../dto/friend-list.dto';

@Component({
  selector: 'app-friend-list',
  standalone: true,
  templateUrl: './friend-list.html',
  styleUrl: './friend-list.css'
})
export class FriendListComponent implements OnInit {
  private friendService = inject(FriendListService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  public friendList = signal<Friend[]>([]);

  ngOnInit(): void {
    this.friendService.getFriendList()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (friends) => this.friendList.set(friends),
        error: (err) => console.error('failed to load', err)
      });
  }

  addFriend(userName: string): void {
    this.friendService.addFriend(userName)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (newFriend) => {
          this.friendList.update(list => [...list, newFriend]);
        },
        error: (err) => console.error('failed to add ', err)
      });
  }

  removeFriend(userName: string): void {
    this.friendService.removeFromFriendList(userName)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.friendList.update(list => list.filter(f => f.userName !== userName));
        },
        error: (err) => console.error('failed to remove', err)
      });
  }

  navigateToChat(userName: string): void {
    this.router.navigate(['/chat', userName]);
  }
}
