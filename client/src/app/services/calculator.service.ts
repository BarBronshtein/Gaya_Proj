import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './operations.service';
import { CalculationRequestDto, CalculationResponseDto } from '../models/operation.model';

@Injectable({
  providedIn: 'root'
})
export class CalculatorService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  /**
   * Executes a 2-operand calculation and records history/metrics (POST /api/calculate)
   */
  calculate(req: CalculationRequestDto): Observable<CalculationResponseDto> {
    return this.http.post<CalculationResponseDto>(`${this.baseUrl}/calculate`, req);
  }
}
