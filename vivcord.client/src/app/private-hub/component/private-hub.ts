import { Component, inject, signal, OnInit } from '@angular/core';
import { PrivateHubService } from '../service/private-hub.service';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { environment } from '@environments/environment';
@Component({
  selector: 'app-private-hub',
  standalone: true,
  templateUrl: './private-hub.html',
  styleUrl: './private-hub.css',
})
export class PrivateHubComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private http = inject(HttpClient);
  private chatService = inject(PrivateHubService);

  public targetUserId = signal<string | null>(null);
  public currentUsername = signal<string>('');
  public messages = this.chatService.messages;

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const username = params.get('username');
      if (username) {
        this.currentUsername.set(username);
        this.loadUserProfile(username);
      }
    });

    this.chatService.connectToHub();
  }

  private loadUserProfile(username: string) {
    this.http.get<{ id: string, userName: string }>(`${environment.apiUrl}/Messaging/find/${username}`)
      .subscribe({
        next: (user) => {
          this.targetUserId.set(user.id);
          this.loadChatHistory(user.id);
        },
        error: () => console.error('User not found')
      });
  }
  private loadChatHistory(targetId: string) {
    this.http.get<{ senderId: string, text: string }[]>(`${environment.apiUrl}/Messaging/history/${targetId}`)
      .subscribe({
        next: (history) => {
          this.chatService.messages.set(history);
        },
        error: (err) => console.error(err)
      });
  }
  public send(text: string) {
    const id = this.targetUserId();
    if (id && text.trim()) {
      this.chatService.sendMessage(id, text);
    }
  }
}
