import { Routes } from '@angular/router';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { DlqViewerComponent } from './components/dlq-viewer/dlq-viewer.component';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'dlq', component: DlqViewerComponent },
  { path: '**', redirectTo: '/dashboard' }
];