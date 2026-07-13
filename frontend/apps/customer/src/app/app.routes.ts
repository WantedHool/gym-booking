import { Route } from '@angular/router';
import { authGuard } from '@frontend/auth';
import { CustomerShell } from './shell/customer-shell';
import { Dashboard } from './dashboard/dashboard';
import { Login } from './login/login';
import { Register } from './register/register';
import { Schedule } from './schedule/schedule';

export const appRoutes: Route[] = [
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  {
    path: '',
    component: CustomerShell,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: Dashboard },
      { path: 'schedule', component: Schedule },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
