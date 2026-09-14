import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { OperationsService } from '../../services/operations.service';
import { OperationDto, CreateOperationDto } from '../../models/operation.model';

@Component({
  selector: 'app-operations-hub',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './operations-hub.component.html',
  styleUrl: './operations-hub.component.css'
})
export class OperationsHubComponent implements OnInit {
  private readonly operationsService = inject(OperationsService);
  private readonly router = inject(Router);

  operations = signal<OperationDto[]>([]);
  isLoading = signal<boolean>(false);
  isSubmitting = signal<boolean>(false);
  successMessage = signal<string | null>(null);
  errorMessage = signal<string | null>(null);
  searchTerm = signal<string>('');
  selectedCategory = signal<string>('ALL');

  categories: string[] = ['Dynamic', 'Arithmetic', 'String', 'ExternalApi'];

  // New Operation Form Model
  newOp: CreateOperationDto = {
    key: '',
    displayName: '',
    category: 'Dynamic',
    ruleTemplate: '',
    fieldAPrompt: '',
    fieldBPrompt: '',
    description: '',
    isActive: true
  };

  filteredOperations = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const cat = this.selectedCategory();
    let list = this.operations();

    if (cat !== 'ALL') {
      list = list.filter(o => (o.category || '').toLowerCase() === cat.toLowerCase());
    }

    if (term) {
      list = list.filter(o =>
        o.key.toLowerCase().includes(term) ||
        o.displayName.toLowerCase().includes(term) ||
        (o.description || '').toLowerCase().includes(term) ||
        (o.ruleTemplate || '').toLowerCase().includes(term)
      );
    }

    return list;
  });

  ngOnInit(): void {
    this.loadOperations();
  }

  loadOperations(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.operationsService.getOperations(false).subscribe({
      next: (ops) => {
        this.isLoading.set(false);
        this.operations.set(ops);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set('Failed to load operations list from backend API / נכשל בטעינת רשימת הפעולות.');
        console.error('Error loading operations:', err);
      }
    });
  }

  onSubmit(): void {
    if (!this.newOp.key || !this.newOp.displayName) {
      this.errorMessage.set('Please provide both Operation Key and Display Name.');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const dto: CreateOperationDto = {
      key: this.newOp.key.trim().toLowerCase(),
      displayName: this.newOp.displayName.trim(),
      category: this.newOp.category || 'Dynamic',
      ruleTemplate: (this.newOp.ruleTemplate || '').trim(),
      fieldAPrompt: (this.newOp.fieldAPrompt || '').trim(),
      fieldBPrompt: (this.newOp.fieldBPrompt || '').trim(),
      description: this.newOp.description?.trim() || null,
      isActive: this.newOp.isActive ?? true
    };

    this.operationsService.createOperation(dto).subscribe({
      next: (created) => {
        this.isSubmitting.set(false);
        this.successMessage.set(`Operation '${created.displayName}' (${created.key}) created successfully and is live!`);
        this.resetForm();
        this.loadOperations();
      },
      error: (err) => {
        this.isSubmitting.set(false);
        const errorDetail = err?.error?.detail || err?.message || 'Failed to create operation.';
        this.errorMessage.set(`Error: ${errorDetail}`);
        console.error('Error creating operation:', err);
      }
    });
  }

  toggleStatus(op: OperationDto): void {
    const nextStatus = !op.isActive;
    this.operationsService.setOperationStatus(op.key, nextStatus).subscribe({
      next: () => {
        this.loadOperations();
      },
      error: (err) => {
        this.errorMessage.set(`Failed to update status for '${op.key}'.`);
        console.error('Failed to update status:', err);
      }
    });
  }

  resetForm(): void {
    this.newOp = {
      key: '',
      displayName: '',
      category: 'Dynamic',
      ruleTemplate: '',
      fieldAPrompt: '',
      fieldBPrompt: '',
      description: '',
      isActive: true
    };
  }

  testInCalculator(key: string): void {
    this.router.navigate(['/calculator'], { queryParams: { key } });
  }

  fillExample(type: 'discount' | 'celsius' | 'greeting' | 'hypotenuse'): void {
    if (type === 'discount') {
      this.newOp = {
        key: 'discount-calc',
        displayName: 'Discount Percentage Calculator',
        category: 'Dynamic',
        ruleTemplate: 'A * (1 - (B / 100))',
        fieldAPrompt: 'Original Price (₪)',
        fieldBPrompt: 'Discount (%)',
        description: 'Calculates final price after percentage discount',
        isActive: true
      };
    } else if (type === 'celsius') {
      this.newOp = {
        key: 'celsius-to-fahrenheit',
        displayName: 'Celsius to Fahrenheit',
        category: 'Dynamic',
        ruleTemplate: '(A * 1.8) + 32',
        fieldAPrompt: 'Degrees Celsius (°C)',
        fieldBPrompt: 'Unused (Enter 0)',
        description: 'Converts Celsius to Fahrenheit: F = (C * 1.8) + 32',
        isActive: true
      };
    } else if (type === 'greeting') {
      this.newOp = {
        key: 'custom-greeting',
        displayName: 'Personalized Greeting',
        category: 'String',
        ruleTemplate: 'Hello {A}, welcome to {B}!',
        fieldAPrompt: 'User Name',
        fieldBPrompt: 'Platform Name',
        description: 'Creates a dynamic welcome greeting',
        isActive: true
      };
    } else if (type === 'hypotenuse') {
      this.newOp = {
        key: 'hypotenuse',
        displayName: 'Hypotenuse (Pythagorean)',
        category: 'Dynamic',
        ruleTemplate: 'sqrt(A*A + B*B)',
        fieldAPrompt: 'Base Length (a)',
        fieldBPrompt: 'Height (b)',
        description: 'Computes hypotenuse c = sqrt(a^2 + b^2)',
        isActive: true
      };
    }
  }

  getCategoryBadgeClass(category?: string): string {
    switch ((category || '').toLowerCase()) {
      case 'arithmetic':
        return 'badge-arithmetic';
      case 'string':
        return 'badge-string';
      case 'externalapi':
      case 'externalservices':
      case 'weather':
        return 'badge-external';
      case 'dynamic':
        return 'badge-dynamic';
      default:
        return 'badge-default';
    }
  }
}
