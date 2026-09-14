import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { OperationsService, API_BASE_URL } from './operations.service';
import { OperationDto, CreateOperationDto, OperationMetricsDto, OperationHistoryDto } from '../models/operation.model';

describe('OperationsService', () => {
  let service: OperationsService;
  let httpMock: HttpTestingController;
  const testBaseUrl = 'http://localhost:5000/api';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        OperationsService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: testBaseUrl }
      ]
    });
    service = TestBed.inject(OperationsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch operations list via GET /api/operations', () => {
    const mockOps: OperationDto[] = [
      {
        key: 'add',
        displayName: 'Addition',
        category: 'Arithmetic',
        ruleTemplate: 'a + b',
        fieldAPrompt: 'Operand A',
        fieldBPrompt: 'Operand B',
        isActive: true
      }
    ];

    service.getOperations().subscribe((ops) => {
      expect(ops.length).toBe(1);
      expect(ops[0].key).toBe('add');
    });

    const req = httpMock.expectOne(`${testBaseUrl}/operations`);
    expect(req.request.method).toBe('GET');
    req.flush(mockOps);
  });

  it('should create operation via POST /api/operations', () => {
    const createDto: CreateOperationDto = {
      key: 'hypotenuse',
      displayName: 'Hypotenuse',
      category: 'Dynamic',
      ruleTemplate: 'sqrt(a*a + b*b)',
      fieldAPrompt: 'Base',
      fieldBPrompt: 'Height',
      isActive: true
    };

    const returnedOp: OperationDto = {
      ...createDto,
      isActive: true
    };

    service.createOperation(createDto).subscribe((op) => {
      expect(op.key).toBe('hypotenuse');
      expect(op.displayName).toBe('Hypotenuse');
    });

    const req = httpMock.expectOne(`${testBaseUrl}/operations`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(createDto);
    req.flush(returnedOp);
  });

  it('should fetch metrics via GET /api/operations/{key}/metrics', () => {
    const mockMetrics: OperationMetricsDto = {
      operationKey: 'add',
      monthlyExecutionCount: 42,
      recentExecutions: [
        {
          id: 1,
          operationKey: 'add',
          fieldA: '10',
          fieldB: '20',
          result: '30',
          durationMs: 5,
          executedAt: '2026-09-14T10:00:00Z'
        }
      ]
    };

    service.getMetrics('add').subscribe((metrics) => {
      expect(metrics.operationKey).toBe('add');
      expect(metrics.monthlyExecutionCount).toBe(42);
      expect(metrics.recentExecutions.length).toBe(1);
    });

    const req = httpMock.expectOne(`${testBaseUrl}/operations/add/metrics`);
    expect(req.request.method).toBe('GET');
    req.flush(mockMetrics);
  });

  it('should fetch history via GET /api/operations/history?limit=10', () => {
    const mockHistory: OperationHistoryDto[] = [
      {
        id: 1,
        operationKey: 'add',
        fieldA: '5',
        fieldB: '5',
        result: '10',
        durationMs: 2,
        executedAt: '2026-09-14T11:00:00Z'
      }
    ];

    service.getHistory(10).subscribe((hist) => {
      expect(hist.length).toBe(1);
      expect(hist[0].result).toBe('10');
    });

    const req = httpMock.expectOne(`${testBaseUrl}/operations/history?limit=10`);
    expect(req.request.method).toBe('GET');
    req.flush(mockHistory);
  });

  it('should update operation status via PUT /api/operations/{key}/status', () => {
    service.setOperationStatus('add', false).subscribe();

    const req = httpMock.expectOne(`${testBaseUrl}/operations/add/status`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ isActive: false });
    req.flush(null);
  });
});
