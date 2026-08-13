import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { FriendListComponent } from '../friend-list/component/friend-list';

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

  ngOnInit() {
    if (this.router.url === '/') {
      const lastChat = localStorage.getItem('lastChat');
      if (lastChat) {
        this.router.navigate(['/chat', lastChat]);
      }
    }
  }
}
