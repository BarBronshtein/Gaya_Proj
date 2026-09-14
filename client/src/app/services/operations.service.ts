import { Injectable, inject, InjectionToken } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  OperationDto,
  CreateOperationDto,
  OperationMetricsDto,
  OperationHistoryDto,
  CalculationRequestDto,
  CalculationResponseDto
} from '../models/operation.model';

export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => environment.apiUrl || 'http://localhost:5000/api'
});

@Injectable({
  providedIn: 'root'
})
export class OperationsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  /**
   * Retrieves all operations (GET /api/operations)
   * @param activeOnly optional filter for active operations only
   */
  getOperations(activeOnly: boolean = false): Observable<OperationDto[]> {
    let params = new HttpParams();
    if (activeOnly) {
      params = params.set('activeOnly', 'true');
    }
    return this.http.get<OperationDto[]>(`${this.baseUrl}/operations`, { params });
  }

  /**
   * Retrieves a single operation by key (GET /api/operations/{key})
   */
  getOperationByKey(key: string): Observable<OperationDto> {
    return this.http.get<OperationDto>(`${this.baseUrl}/operations/${encodeURIComponent(key)}`);
  }

  /**
   * Dynamically creates or updates an operation (POST /api/operations)
   */
  createOperation(dto: CreateOperationDto): Observable<OperationDto> {
    return this.http.post<OperationDto>(`${this.baseUrl}/operations`, dto);
  }

  /**
   * Retrieves operation metrics: monthly execution count + 3 most recent executions (GET /api/operations/{key}/metrics)
   */
  getMetrics(key: string): Observable<OperationMetricsDto> {
    return this.http.get<OperationMetricsDto>(`${this.baseUrl}/operations/${encodeURIComponent(key)}/metrics`);
  }

  /**
   * Retrieves recent execution history across all operations (GET /api/operations/history)
   */
  getHistory(limit: number = 50): Observable<OperationHistoryDto[]> {
    const params = new HttpParams().set('limit', limit.toString());
    return this.http.get<OperationHistoryDto[]>(`${this.baseUrl}/operations/history`, { params });
  }

  /**
   * Toggles the active status of an operation (PUT /api/operations/{key}/status)
   */
  setOperationStatus(key: string, isActive: boolean): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/operations/${encodeURIComponent(key)}/status`, { isActive });
  }

  /**
   * Alias for setOperationStatus
   */
  setStatus(key: string, isActive: boolean): Observable<void> {
    return this.setOperationStatus(key, isActive);
  }

  /**
   * Executes a 2-operand calculation and records history/metrics (POST /api/calculate)
   */
  calculate(request: CalculationRequestDto): Observable<CalculationResponseDto> {
    return this.http.post<CalculationResponseDto>(`${this.baseUrl}/calculate`, request);
  }
}
