export interface OperationDto {
  key: string;
  displayName: string;
  category: string;
  ruleTemplate: string;
  fieldAPrompt: string;
  fieldBPrompt: string;
  description?: string | null;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string | null;
}

export interface CreateOperationDto {
  key: string;
  displayName: string;
  category: string;
  ruleTemplate: string;
  fieldAPrompt: string;
  fieldBPrompt: string;
  description?: string | null;
  isActive?: boolean;
}

export interface CalculationRequestDto {
  operationKey: string;
  fieldA: string;
  fieldB: string;
}

export interface OperationHistoryDto {
  id?: number;
  operationKey: string;
  fieldA: string;
  fieldB: string;
  result: string;
  durationMs: number;
  executedAt: string;
}

export interface CalculationResponseDto {
  operationKey: string;
  fieldA: string;
  fieldB: string;
  result: string;
  durationMs: number;
  executedAt: string;
  recentExecutions: OperationHistoryDto[];
  monthlyExecutionCount: number;
}

export interface OperationMetricsDto {
  operationKey: string;
  monthlyExecutionCount: number;
  recentExecutions: OperationHistoryDto[];
}

export interface WeatherData {
  latitude?: number;
  longitude?: number;
  temperature?: number;
  humidity?: number;
  windSpeed?: number;
  weatherCode?: number;
  weatherCondition?: string;
  weatherIcon?: string;
  time?: string;
}

export interface ParsedJsonEntry {
  key: string;
  value: string;
  isObject: boolean;
}
