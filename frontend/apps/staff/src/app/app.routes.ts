import { Route } from '@angular/router';
import { authGuard, roleGuard } from '@frontend/auth';
import { ClassTypes } from './class-types/class-types';
import { Dashboard } from './dashboard/dashboard';
import { Login } from './login/login';
import { Register } from './register/register';
import { Sessions } from './sessions/sessions';
import { StaffShell } from './shell/staff-shell';

export const appRoutes: Route[] = [
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  {
    path: '',
    component: StaffShell,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: Dashboard },
      { path: 'class-types', component: ClassTypes, canActivate: [roleGuard('Instructor', 'Admin')] },
      { path: 'sessions', component: Sessions, canActivate: [roleGuard('Instructor', 'Admin')] },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
