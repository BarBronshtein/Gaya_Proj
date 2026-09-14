import { Routes } from '@angular/router';
import { CalculatorComponent } from './components/calculator/calculator.component';
import { OperationsHubComponent } from './components/operations-hub/operations-hub.component';

export const routes: Routes = [
  { path: '', redirectTo: 'calculator', pathMatch: 'full' },
  { path: 'calculator', component: CalculatorComponent },
  { path: 'operations-hub', component: OperationsHubComponent },
  { path: '**', redirectTo: 'calculator' }
];
