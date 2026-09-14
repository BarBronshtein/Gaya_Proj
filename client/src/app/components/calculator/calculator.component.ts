import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { OperationsService } from '../../services/operations.service';
import { CalculatorService } from '../../services/calculator.service';
import {
  OperationDto,
  CalculationResponseDto,
  OperationHistoryDto
} from '../../models/operation.model';
import { formatIsraelDateTime } from '../../utils/date.utils';
import { IsraelDatePipe } from '../../pipes/israel-date.pipe';

@Component({
  selector: 'app-calculator',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink, IsraelDatePipe],
  templateUrl: './calculator.component.html',
  styleUrl: './calculator.component.css'
})
export class CalculatorComponent implements OnInit {
  readonly operationsService = inject(OperationsService);
  readonly calculatorService = inject(CalculatorService);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);

  // State signals
  operations = signal<OperationDto[]>([]);
  isLoadingOperations = signal<boolean>(true);
  selectedOperationKey = signal<string>('');
  isCalculating = signal<boolean>(false);
  isLoadingMetrics = signal<boolean>(false);
  calculationResult = signal<CalculationResponseDto | null>(null);
  recentExecutions = signal<OperationHistoryDto[]>([]);
  monthlyExecutionCount = signal<number>(0);
  errorMessage = signal<string | null>(null);
  copied = signal<boolean>(false);

  // Reactive Form
  calcForm = this.fb.group({
    operationKey: ['', [Validators.required]],
    fieldA: ['', [Validators.required]],
    fieldB: ['', [Validators.required]]
  });

  // Computed properties
  selectedOperation = computed(() => {
    const key = this.selectedOperationKey();
    return this.operations().find(op => op.key === key) || null;
  });

  fieldALabel = computed(() => {
    const op = this.selectedOperation();
    return op?.fieldAPrompt || 'Operand A (שדה א\')';
  });

  fieldBLabel = computed(() => {
    const op = this.selectedOperation();
    return op?.fieldBPrompt || 'Operand B (שדה ב\')';
  });

  categories = computed(() => {
    const ops = this.operations();
    const map = new Map<string, OperationDto[]>();
    for (const op of ops) {
      const cat = op.category || 'Other';
      if (!map.has(cat)) {
        map.set(cat, []);
      }
      map.get(cat)!.push(op);
    }
    return Array.from(map.entries()).map(([category, items]) => ({ category, items }));
  });

  ngOnInit(): void {
    this.loadOperations();
  }

  loadOperations(): void {
    this.isLoadingOperations.set(true);
    this.errorMessage.set(null);

    this.operationsService.getOperations().subscribe({
      next: (ops) => {
        this.operations.set(ops);
        this.isLoadingOperations.set(false);

        // Check if query param 'key' is provided (e.g. from Operations Hub)
        const queryKey = this.route.snapshot.queryParamMap.get('key');
        if (queryKey && ops.some(o => o.key === queryKey)) {
          this.selectOperation(queryKey);
        } else if (ops.length > 0) {
          const defaultOp = ops.find(o => o.key === 'add') || ops[0];
          this.selectOperation(defaultOp.key);
        }
      },
      error: (err) => {
        this.isLoadingOperations.set(false);
        this.errorMessage.set('Failed to load operations from server. Please verify backend API is running.');
        console.error('Error loading operations:', err);
      }
    });
  }

  onOperationChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.selectOperation(select.value);
  }

  selectOperation(key: string): void {
    this.selectedOperationKey.set(key);
    this.calcForm.patchValue({ operationKey: key });

    // Set intelligent defaults for weather
    if (key === 'weather') {
      const currentA = this.calcForm.get('fieldA')?.value;
      const currentB = this.calcForm.get('fieldB')?.value;
      if (!currentA) this.calcForm.patchValue({ fieldA: '32.0853' }); // Tel Aviv latitude
      if (!currentB) this.calcForm.patchValue({ fieldB: '34.7818' }); // Tel Aviv longitude
    }

    this.loadOperationMetrics(key);
  }

  loadOperationMetrics(key: string): void {
    if (!key) return;
    this.isLoadingMetrics.set(true);

    this.operationsService.getMetrics(key).subscribe({
      next: (metrics) => {
        this.monthlyExecutionCount.set(metrics.monthlyExecutionCount);
        this.recentExecutions.set(metrics.recentExecutions || []);
        this.isLoadingMetrics.set(false);
      },
      error: () => {
        this.isLoadingMetrics.set(false);
        this.monthlyExecutionCount.set(0);
        this.recentExecutions.set([]);
      }
    });
  }

  calculate(): void {
    this.onCalculate();
  }

  onCalculate(): void {
    if (this.calcForm.invalid) {
      this.calcForm.markAllAsTouched();
      return;
    }

    const { operationKey, fieldA, fieldB } = this.calcForm.getRawValue();
    if (!operationKey) return;

    this.isCalculating.set(true);
    this.errorMessage.set(null);

    this.calculatorService.calculate({
      operationKey,
      fieldA: fieldA ?? '',
      fieldB: fieldB ?? ''
    }).subscribe({
      next: (response) => {
        this.isCalculating.set(false);
        this.calculationResult.set(response);
        this.monthlyExecutionCount.set(response.monthlyExecutionCount);
        this.recentExecutions.set(response.recentExecutions || []);
      },
      error: (err) => {
        this.isCalculating.set(false);
        const detail = err.error?.detail || err.error?.title || err.message || 'Calculation failed.';
        this.errorMessage.set(`Calculation error: ${detail}`);
        console.error('Calculation error:', err);
      }
    });
  }

  applyPreset(key: string, a: string, b: string): void {
    this.selectOperation(key);
    this.calcForm.patchValue({
      operationKey: key,
      fieldA: a,
      fieldB: b
    });
  }

  copyResult(): void {
    const res = this.calculationResult();
    if (!res) return;
    navigator.clipboard.writeText(res.result).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    });
  }

  formatDate(dateStr: string): string {
    return formatIsraelDateTime(dateStr);
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
