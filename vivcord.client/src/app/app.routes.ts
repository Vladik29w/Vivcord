import { Routes } from '@angular/router';
import { HomeComponent } from '../app/home-component/home-component';
import { PrivateHubComponent } from '../app/private-hub/component/private-hub'
import { FriendListComponent } from '../app/friend-list/component/friend-list';
import { LoginComponent } from '../app/account/components/login.component/login';
import { RegisterComponent } from '../app/account/components/register.component/register';

export const routes: Routes = [
  {
    path: '',
    component: HomeComponent,
    children: [
      {
        path: 'chat/:username',
        component: PrivateHubComponent
      }
    ]
  },
  {
    path: 'friends',
    component: FriendListComponent
  },
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: 'register',
    component: RegisterComponent
  }
];
