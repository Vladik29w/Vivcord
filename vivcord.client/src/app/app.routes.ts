import { Routes } from '@angular/router';
import { HomeComponent } from '../app/home-component/home-component';
import { PrivateHubComponent } from '../app/private-hub/component/private-hub';
import { FriendListComponent } from '../app/friend-list/component/friend-list';
import { LoginComponent } from '../app/account/components/login.component/login';
import { RegisterComponent } from '../app/account/components/register.component/register';
import { VoiceChatComponent } from './voice-chat/component/voice-chat/voice-chat';
import { GroupHubComponent } from './group-hub/component/group-hub';
import { authGuard, guestGuard } from './auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent,
    canActivate: [guestGuard]
  },
  {
    path: 'register',
    component: RegisterComponent,
    canActivate: [guestGuard]
  },
  {
    path: '',
    component: HomeComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'chat/:username',
        component: PrivateHubComponent
      },
      {
        path: 'group/:groupId',
        component: GroupHubComponent
      }
    ]
  },
  {
    path: 'friends',
    component: FriendListComponent,
    canActivate: [authGuard]
  },
  {
    path: 'voice-chat/:roomName',
    component: VoiceChatComponent,
    canActivate: [authGuard]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
