import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CalculatorService } from './calculator.service';
import { API_BASE_URL } from './operations.service';
import { CalculationRequestDto, CalculationResponseDto } from '../models/operation.model';

describe('CalculatorService', () => {
  let service: CalculatorService;
  let httpMock: HttpTestingController;
  const testBaseUrl = 'http://localhost:5000/api';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        CalculatorService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: testBaseUrl }
      ]
    });
    service = TestBed.inject(CalculatorService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should post calculation request to /api/calculate', () => {
    const requestDto: CalculationRequestDto = {
      operationKey: 'add',
      fieldA: '25',
      fieldB: '75'
    };

    const mockResponse: CalculationResponseDto = {
      operationKey: 'add',
      fieldA: '25',
      fieldB: '75',
      result: '100',
      durationMs: 3,
      executedAt: '2026-09-14T12:00:00Z',
      recentExecutions: [],
      monthlyExecutionCount: 15
    };

    service.calculate(requestDto).subscribe((res) => {
      expect(res.result).toBe('100');
      expect(res.operationKey).toBe('add');
      expect(res.monthlyExecutionCount).toBe(15);
    });

    const req = httpMock.expectOne(`${testBaseUrl}/calculate`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(requestDto);
    req.flush(mockResponse);
  });
});
