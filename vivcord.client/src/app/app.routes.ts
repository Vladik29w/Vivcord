import { Routes } from '@angular/router';
import { HomeComponent } from '../app/home-component/home-component';
import { PrivateHubComponent } from '../app/private-hub/component/private-hub'
import { FriendListComponent } from '../app/friend-list/component/friend-list';

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
  }
];
