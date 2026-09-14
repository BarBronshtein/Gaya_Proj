import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { CalculatorComponent } from './calculator.component';
import { OperationsService } from '../../services/operations.service';
import { CalculatorService } from '../../services/calculator.service';
import { OperationDto, CalculationResponseDto, OperationMetricsDto } from '../../models/operation.model';

describe('CalculatorComponent', () => {
  let component: CalculatorComponent;
  let fixture: ComponentFixture<CalculatorComponent>;
  let mockOperationsService: jasmine.SpyObj<OperationsService>;
  let mockCalculatorService: jasmine.SpyObj<CalculatorService>;

  const mockOps: OperationDto[] = [
    {
      key: 'add',
      displayName: 'Addition',
      category: 'Arithmetic',
      ruleTemplate: 'a + b',
      fieldAPrompt: 'First Number',
      fieldBPrompt: 'Second Number',
      isActive: true
    },
    {
      key: 'weather',
      displayName: 'Weather Forecast',
      category: 'ExternalApi',
      ruleTemplate: 'Open-Meteo',
      fieldAPrompt: 'Latitude',
      fieldBPrompt: 'Longitude',
      isActive: true
    }
  ];

  const mockMetrics: OperationMetricsDto = {
    operationKey: 'add',
    monthlyExecutionCount: 5,
    recentExecutions: [
      {
        id: 1,
        operationKey: 'add',
        fieldA: '1',
        fieldB: '2',
        result: '3',
        durationMs: 1,
        executedAt: '2026-09-14T08:00:00Z'
      }
    ]
  };

  beforeEach(async () => {
    mockOperationsService = jasmine.createSpyObj('OperationsService', ['getOperations', 'getMetrics']);
    mockCalculatorService = jasmine.createSpyObj('CalculatorService', ['calculate']);

    mockOperationsService.getOperations.and.returnValue(of(mockOps));
    mockOperationsService.getMetrics.and.returnValue(of(mockMetrics));

    await TestBed.configureTestingModule({
      imports: [CalculatorComponent],
      providers: [
        provideRouter([]),
        { provide: OperationsService, useValue: mockOperationsService },
        { provide: CalculatorService, useValue: mockCalculatorService },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: {
                get: (param: string) => null
              }
            }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CalculatorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component and load operations', () => {
    expect(component).toBeTruthy();
    expect(mockOperationsService.getOperations).toHaveBeenCalled();
    expect(component.operations().length).toBe(2);
  });

  it('should dynamically update contextual prompts when operation changes', () => {
    // Initially selected 'add'
    expect(component.fieldALabel()).toBe('First Number');
    expect(component.fieldBLabel()).toBe('Second Number');

    // Switch to 'weather'
    component.selectOperation('weather');
    fixture.detectChanges();

    expect(component.fieldALabel()).toBe('Latitude');
    expect(component.fieldBLabel()).toBe('Longitude');
  });

  it('should display monthly execution count badge', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const badge = compiled.querySelector('#monthly-count-badge');
    expect(badge).toBeTruthy();
    expect(badge?.textContent).toContain('5');
  });

  it('should display recent executions in table', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const table = compiled.querySelector('#recent-executions-table');
    expect(table).toBeTruthy();
    expect(table?.textContent).toContain('1');
    expect(table?.textContent).toContain('2');
    expect(table?.textContent).toContain('3');
  });

  it('should execute calculation on onCalculate and display result card', () => {
    const mockCalcResponse: CalculationResponseDto = {
      operationKey: 'add',
      fieldA: '10',
      fieldB: '20',
      result: '30',
      durationMs: 4,
      executedAt: '2026-09-14T12:00:00Z',
      recentExecutions: [],
      monthlyExecutionCount: 6
    };
    mockCalculatorService.calculate.and.returnValue(of(mockCalcResponse));

    component.calcForm.patchValue({
      operationKey: 'add',
      fieldA: '10',
      fieldB: '20'
    });

    component.onCalculate();
    fixture.detectChanges();

    expect(mockCalculatorService.calculate).toHaveBeenCalledWith({
      operationKey: 'add',
      fieldA: '10',
      fieldB: '20'
    });

    expect(component.calculationResult()?.result).toBe('30');
    expect(component.monthlyExecutionCount()).toBe(6);

    const compiled = fixture.nativeElement as HTMLElement;
    const resultCard = compiled.querySelector('#calculation-result-card');
    expect(resultCard).toBeTruthy();
    expect(resultCard?.textContent).toContain('30');
    expect(resultCard?.textContent).toContain('4 ms');
    // Result card timestamp in Israel time (12:00 UTC -> 15:00 IDT)
    expect(resultCard?.textContent).toContain('15:00:00');
    expect(resultCard?.textContent).toContain('Israel Time');
  });

  it('should format timestamps in Israel local time and not UTC', () => {
    // 2026-09-14T12:00:00Z (Summer UTC+3) -> 15:00:00
    const summerTime = component.formatDate('2026-09-14T12:00:00Z');
    expect(summerTime).toBe('14/09/2026, 15:00:00 (Israel Time)');
    expect(summerTime).not.toContain('UTC');

    // 2026-01-15T10:00:00Z (Winter UTC+2) -> 12:00:00
    const winterTime = component.formatDate('2026-01-15T10:00:00Z');
    expect(winterTime).toBe('15/01/2026, 12:00:00 (Israel Time)');
    expect(winterTime).not.toContain('UTC');
  });
});

