import { Routes } from '@angular/router';
import { HomeComponent } from '../app/home-component/home-component';
import { PrivateHubComponent } from '../app/private-hub/component/private-hub'
import { FriendListComponent } from '../app/friend-list/component/friend-list';
import { LoginComponent } from '../app/account/login.component/login';
import { RegisterComponent } from '../app/account/register.component/register';

export const routes: Routes = [
  {
    path: '',
    component: HomeComponent
  },
  {
    path: 'chat/:username',
    component: PrivateHubComponent
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
