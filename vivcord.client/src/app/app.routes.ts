import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./account/components/login.component/login').then(m => m.LoginComponent),
    canActivate: [guestGuard]
  },
  {
    path: 'register',
    loadComponent: () => import('./account/components/register.component/register').then(m => m.RegisterComponent),
    canActivate: [guestGuard]
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./account/components/forgot-password/forgot-password').then(m => m.ForgotPasswordComponent),
    canActivate: [guestGuard]
  },
  {
    path: 'reset-password',
    loadComponent: () => import('./account/components/reset-password/reset-password').then(m => m.ResetPasswordComponent),
    canActivate: [guestGuard]
  },
  {
    path: '',
    loadComponent: () => import('./home-component/home-component').then(m => m.HomeComponent),
    canActivate: [authGuard],
    children: [
      {
        path: 'chat/:username',
        loadComponent: () => import('./private-hub/component/private-hub').then(m => m.PrivateHubComponent)
      },
      {
        path: 'group/:groupId',
        loadComponent: () => import('./group-hub/component/group-hub').then(m => m.GroupHubComponent)
      },
      {
        path: 'profile',
        loadComponent: () => import('./profile/component/profile').then(m => m.Profile)
      },
      {
        path: 'voice-chat',
        loadComponent: () => import('./voice-chat/component/voice-chat-room/voice-chat-room').then(m => m.VoiceChatRoomComponent)
      }
    ]
  },
  {
    path: 'friends',
    loadComponent: () => import('./friend-list/component/friend-list').then(m => m.FriendListComponent),
    canActivate: [authGuard]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
