import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { OperationsService } from '../../services/operations.service';
import { CalculatorService } from '../../services/calculator.service';
import {
  OperationDto,
  CalculationResponseDto,
  OperationHistoryDto,
  WeatherData,
  ParsedJsonEntry
} from '../../models/operation.model';
import { formatIsraelDateTime } from '../../utils/date.utils';
import { IsraelDatePipe } from '../../pipes/israel-date.pipe';

export interface CityPreset {
  name: string;
  hebrew: string;
  lat: string;
  lon: string;
  flag: string;
}

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
  showRawJson = signal<boolean>(false);
  viewMode = signal<'visual' | 'formatted' | 'raw'>('visual');

  readonly cityPresets: CityPreset[] = [
    { name: 'Tel Aviv', hebrew: 'תל אביב', lat: '32.0853', lon: '34.7818', flag: '🇮🇱' },
    { name: 'Jerusalem', hebrew: 'ירושלים', lat: '31.7683', lon: '35.2137', flag: '🇮🇱' },
    { name: 'Haifa', hebrew: 'חיפה', lat: '32.7940', lon: '34.9896', flag: '🇮🇱' },
    { name: 'Eilat', hebrew: 'אילת', lat: '29.5577', lon: '34.9519', flag: '🇮🇱' },
    { name: 'New York', hebrew: 'ניו יורק', lat: '40.7128', lon: '-74.0060', flag: '🇺🇸' },
    { name: 'London', hebrew: 'לונדון', lat: '51.5074', lon: '-0.1278', flag: '🇬🇧' },
    { name: 'Tokyo', hebrew: 'טוקיו', lat: '35.6762', lon: '139.6503', flag: '🇯🇵' },
    { name: 'Paris', hebrew: 'פריז', lat: '48.8566', lon: '2.3522', flag: '🇫🇷' }
  ];

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

  isExternalApi = computed(() => {
    const op = this.selectedOperation();
    if (!op) return false;
    const cat = (op.category || '').toLowerCase();
    const tmpl = (op.ruleTemplate || '').toLowerCase();
    return cat === 'externalapi' || cat === 'externalservices' || cat === 'weather' || tmpl.startsWith('http://') || tmpl.startsWith('https://');
  });

  isWeatherOperation = computed(() => {
    const op = this.selectedOperation();
    return (this.selectedOperationKey() || '').toLowerCase() === 'weather' || (op?.key || '').toLowerCase() === 'weather';
  });

  resolvedUrlPreview = computed(() => {
    const op = this.selectedOperation();
    if (!op) return '';
    const tmpl = op.ruleTemplate || '';
    if (!tmpl.startsWith('http://') && !tmpl.startsWith('https://')) return '';

    const rawA = this.calcForm.get('fieldA')?.value;
    const rawB = this.calcForm.get('fieldB')?.value;
    const valA = (rawA !== null && rawA !== undefined && rawA !== '') ? encodeURIComponent(rawA) : '{A}';
    const valB = (rawB !== null && rawB !== undefined && rawB !== '') ? encodeURIComponent(rawB) : '{B}';

    return tmpl.replace(/{A}/gi, valA).replace(/{B}/gi, valB);
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

  isJsonResult = computed(() => {
    const res = this.calculationResult();
    if (!res || !res.result) return false;
    const trimmed = res.result.trim();
    return (trimmed.startsWith('{') && trimmed.endsWith('}')) || (trimmed.startsWith('[') && trimmed.endsWith(']'));
  });

  formattedResult = computed(() => {
    const res = this.calculationResult();
    if (!res || !res.result) return '';
    const trimmed = res.result.trim();
    if ((trimmed.startsWith('{') && trimmed.endsWith('}')) || (trimmed.startsWith('[') && trimmed.endsWith(']'))) {
      try {
        const parsed = JSON.parse(trimmed);
        return JSON.stringify(parsed, null, 2);
      } catch {
        return res.result;
      }
    }
    return res.result;
  });

  isWeatherResult = computed(() => {
    const res = this.calculationResult();
    if (!res || !res.result) return false;
    if (res.operationKey.toLowerCase() === 'weather') return true;
    const trimmed = res.result.trim();
    return trimmed.startsWith('{') && (trimmed.includes('temperature_2m') || trimmed.includes('current_weather'));
  });

  parsedWeatherData = computed<WeatherData | null>(() => {
    if (!this.isWeatherResult()) return null;
    const res = this.calculationResult();
    if (!res || !res.result) return null;
    try {
      const data = JSON.parse(res.result.trim());
      const current = data.current || {};
      const currentWeather = data.current_weather || {};
      const temp = current.temperature_2m ?? currentWeather.temperature;
      const humidity = current.relative_humidity_2m;
      const wind = current.wind_speed_10m ?? currentWeather.windspeed;
      const code = current.weather_code ?? currentWeather.weathercode ?? 0;
      const lat = data.latitude ?? (parseFloat(res.fieldA) || undefined);
      const lon = data.longitude ?? (parseFloat(res.fieldB) || undefined);
      const time = current.time ?? currentWeather.time;

      const { condition, icon } = this.getWeatherCondition(Number(code));

      return {
        temperature: typeof temp === 'number' ? temp : undefined,
        humidity: typeof humidity === 'number' ? humidity : undefined,
        windSpeed: typeof wind === 'number' ? wind : undefined,
        weatherCode: typeof code === 'number' ? code : undefined,
        weatherCondition: condition,
        weatherIcon: icon,
        latitude: lat,
        longitude: lon,
        time: time
      };
    } catch {
      return null;
    }
  });

  parsedGenericJsonEntries = computed<ParsedJsonEntry[]>(() => {
    const res = this.calculationResult();
    if (!res || !res.result || !this.isJsonResult() || this.isWeatherResult()) return [];
    try {
      const data = JSON.parse(res.result.trim());
      if (typeof data !== 'object' || data === null) return [];
      if (Array.isArray(data)) {
        return data.slice(0, 10).map((item, idx) => ({
          key: `[${idx}]`,
          value: typeof item === 'object' ? JSON.stringify(item) : String(item),
          isObject: typeof item === 'object' && item !== null
        }));
      }
      return Object.entries(data).map(([k, v]) => ({
        key: k,
        value: typeof v === 'object' && v !== null ? JSON.stringify(v, null, 2) : String(v),
        isObject: typeof v === 'object' && v !== null
      }));
    } catch {
      return [];
    }
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
        this.viewMode.set('visual');
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

  applyCityPreset(lat: string, lon: string): void {
    this.calcForm.patchValue({
      fieldA: lat,
      fieldB: lon
    });
  }

  setViewMode(mode: 'visual' | 'formatted' | 'raw'): void {
    this.viewMode.set(mode);
    if (mode === 'raw') {
      this.showRawJson.set(true);
    } else {
      this.showRawJson.set(false);
    }
  }

  copyResult(): void {
    const res = this.calculationResult();
    if (!res) return;
    let textToCopy = res.result;
    if (this.viewMode() === 'formatted' && this.isJsonResult()) {
      textToCopy = this.formattedResult();
    } else if (this.viewMode() === 'raw' || this.showRawJson()) {
      textToCopy = res.result;
    } else if (this.isJsonResult()) {
      textToCopy = this.formattedResult();
    }
    navigator.clipboard.writeText(textToCopy).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    });
  }

  getWeatherCondition(code: number): { condition: string; icon: string } {
    if (code === 0) return { condition: 'Clear Sky / בהיר', icon: '☀️' };
    if (code === 1) return { condition: 'Mainly Clear / בהיר ברובו', icon: '🌤️' };
    if (code === 2) return { condition: 'Partly Cloudy / מעונן חלקית', icon: '⛅' };
    if (code === 3) return { condition: 'Overcast / מעונן', icon: '☁️' };
    if (code === 45 || code === 48) return { condition: 'Foggy / ערפילי', icon: '🌫️' };
    if (code >= 51 && code <= 57) return { condition: 'Drizzle / טפטוף', icon: '🌦️' };
    if (code >= 61 && code <= 67) return { condition: 'Rain / גשם', icon: '🌧️' };
    if (code >= 71 && code <= 77) return { condition: 'Snowfall / שלג', icon: '❄️' };
    if (code >= 80 && code <= 82) return { condition: 'Rain Showers / ממטרים', icon: '🌧️' };
    if (code === 85 || code === 86) return { condition: 'Snow Showers / מטר שלג', icon: '🌨️' };
    if (code >= 95) return { condition: 'Thunderstorm / סופת רעמים', icon: '⛈️' };
    return { condition: 'Fair Weather / נאה', icon: '🌤️' };
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
