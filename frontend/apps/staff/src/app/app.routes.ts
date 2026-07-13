import { Route } from '@angular/router';
import { authGuard, roleGuard } from '@frontend/auth';
import { ClassTypes } from './class-types/class-types';
import { ClassTypeNew } from './class-types/class-type-new';
import { Dashboard } from './dashboard/dashboard';
import { Invitations } from './invitations/invitations';
import { Login } from './login/login';
import { Memberships } from './memberships/memberships';
import { PlanNew } from './memberships/plan-new';
import { Register } from './register/register';
import { Sessions } from './sessions/sessions';
import { SessionNew } from './sessions/session-new';
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
      { path: 'class-types/new', component: ClassTypeNew, canActivate: [roleGuard('Instructor', 'Admin')] },
      { path: 'sessions', component: Sessions, canActivate: [roleGuard('Instructor', 'Admin')] },
      { path: 'sessions/new', component: SessionNew, canActivate: [roleGuard('Instructor', 'Admin')] },
      { path: 'invitations', component: Invitations, canActivate: [roleGuard('Admin')] },
      { path: 'memberships', component: Memberships, canActivate: [roleGuard('Admin')] },
      { path: 'memberships/new', component: PlanNew, canActivate: [roleGuard('Admin')] },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
