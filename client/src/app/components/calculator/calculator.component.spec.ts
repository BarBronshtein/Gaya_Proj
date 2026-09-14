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
    },
    {
      key: 'crypto-price',
      displayName: 'Crypto Price',
      category: 'ExternalApi',
      ruleTemplate: 'https://api.coingecko.com/api/v3/simple/price?ids={A}&vs_currencies={B}',
      fieldAPrompt: 'Coin ID',
      fieldBPrompt: 'Currency',
      isActive: true
    },
    {
      key: 'predict-age',
      displayName: 'Predict Age',
      category: 'ExternalApi',
      ruleTemplate: 'https://api.agify.io/?name={A}&country_id={B}',
      fieldAPrompt: 'Name',
      fieldBPrompt: 'Country',
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
    expect(component.operations().length).toBe(4);
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

  it('should detect and format JSON result for external API calculations', () => {
    const rawJson = '{"latitude":32.0853,"current":{"temperature_2m":24.5}}';
    const mockCalcResponse: CalculationResponseDto = {
      operationKey: 'weather',
      fieldA: '32.0853',
      fieldB: '34.7818',
      result: rawJson,
      durationMs: 45,
      executedAt: '2026-09-14T12:00:00Z',
      recentExecutions: [],
      monthlyExecutionCount: 1
    };
    mockCalculatorService.calculate.and.returnValue(of(mockCalcResponse));

    component.calcForm.patchValue({
      operationKey: 'weather',
      fieldA: '32.0853',
      fieldB: '34.7818'
    });

    component.onCalculate();
    fixture.detectChanges();

    expect(component.isJsonResult()).toBeTrue();
    expect(component.formattedResult()).toContain('"temperature_2m": 24.5');

    const compiled = fixture.nativeElement as HTMLElement;
    const jsonBlock = compiled.querySelector('.result-json-block');
    expect(jsonBlock).toBeTruthy();
    expect(jsonBlock?.textContent).toContain('temperature_2m');
  });

  it('should render external API live endpoint card and city presets when weather is selected', () => {
    component.selectOperation('weather');
    fixture.detectChanges();

    expect(component.isExternalApi()).toBeTrue();
    expect(component.isWeatherOperation()).toBeTrue();

    const compiled = fixture.nativeElement as HTMLElement;
    const endpointCard = compiled.querySelector('#api-endpoint-card');
    expect(endpointCard).toBeTruthy();
    expect(endpointCard?.textContent).toContain('GET');
    expect(endpointCard?.textContent).toContain('External REST API');

    const cityBar = compiled.querySelector('.city-presets-bar');
    expect(cityBar).toBeTruthy();
    expect(cityBar?.textContent).toContain('Tel Aviv');
    expect(cityBar?.textContent).toContain('Jerusalem');
  });

  it('should apply city coordinates when clicking a city preset button', () => {
    component.selectOperation('weather');
    component.applyCityPreset('31.7683', '35.2137'); // Jerusalem
    fixture.detectChanges();

    expect(component.calcForm.get('fieldA')?.value).toBe('31.7683');
    expect(component.calcForm.get('fieldB')?.value).toBe('35.2137');
  });

  it('should render contextual API presets bar for non-weather external APIs (e.g. crypto-price)', () => {
    component.selectOperation('crypto-price');
    fixture.detectChanges();

    expect(component.isExternalApi()).toBeTrue();
    expect(component.isWeatherOperation()).toBeFalse();
    expect(component.currentApiPresets().length).toBeGreaterThan(0);

    const compiled = fixture.nativeElement as HTMLElement;
    const apiPresetsBar = compiled.querySelector('#api-presets-bar');
    expect(apiPresetsBar).toBeTruthy();
    expect(apiPresetsBar?.textContent).toContain('Bitcoin / USD');
    expect(apiPresetsBar?.textContent).toContain('Ethereum / EUR');

    // Click on preset
    component.applyApiPreset('ethereum', 'eur');
    expect(component.calcForm.get('fieldA')?.value).toBe('ethereum');
    expect(component.calcForm.get('fieldB')?.value).toBe('eur');
  });

  it('should parse weather JSON and render rich weather widget in visual view', () => {
    const fullWeatherJson = JSON.stringify({
      latitude: 32.0853,
      longitude: 34.7818,
      current: {
        time: '2026-09-14T16:00',
        temperature_2m: 26.4,
        relative_humidity_2m: 62,
        wind_speed_10m: 14.8,
        weather_code: 1
      }
    });

    const mockCalcResponse: CalculationResponseDto = {
      operationKey: 'weather',
      fieldA: '32.0853',
      fieldB: '34.7818',
      result: fullWeatherJson,
      durationMs: 38,
      executedAt: '2026-09-14T12:00:00Z',
      recentExecutions: [],
      monthlyExecutionCount: 2
    };
    mockCalculatorService.calculate.and.returnValue(of(mockCalcResponse));

    component.selectOperation('weather');
    component.calcForm.patchValue({
      operationKey: 'weather',
      fieldA: '32.0853',
      fieldB: '34.7818'
    });

    component.onCalculate();
    fixture.detectChanges();

    expect(component.isWeatherResult()).toBeTrue();
    const weatherData = component.parsedWeatherData();
    expect(weatherData).toBeTruthy();
    expect(weatherData?.temperature).toBe(26.4);
    expect(weatherData?.humidity).toBe(62);
    expect(weatherData?.windSpeed).toBe(14.8);
    expect(weatherData?.weatherCondition).toContain('Clear');

    const compiled = fixture.nativeElement as HTMLElement;
    const weatherWidget = compiled.querySelector('#weather-result-widget');
    expect(weatherWidget).toBeTruthy();
    expect(weatherWidget?.textContent).toContain('26.4 °C');
    expect(weatherWidget?.textContent).toContain('62 %');
    expect(weatherWidget?.textContent).toContain('14.8 km/h');
  });

  it('should parse and flatten generic nested JSON results for inspector widget', () => {
    const cryptoJson = JSON.stringify({
      bitcoin: {
        usd: 62500,
        ils: 235000
      }
    });

    component.calculationResult.set({
      operationKey: 'crypto-price',
      fieldA: 'bitcoin',
      fieldB: 'usd',
      result: cryptoJson,
      durationMs: 25,
      executedAt: '2026-09-14T12:00:00Z',
      recentExecutions: [],
      monthlyExecutionCount: 1
    });

    const entries = component.parsedGenericJsonEntries();
    expect(entries.length).toBe(2);
    expect(entries[0].key).toBe('bitcoin → usd');
    expect(entries[0].value).toBe('62500');
    expect(entries[1].key).toBe('bitcoin → ils');
    expect(entries[1].value).toBe('235000');
  });

  it('should allow switching view modes between visual, formatted, and raw', () => {
    const rawJson = '{"result":"data","count":42}';
    component.calculationResult.set({
      operationKey: 'custom-api',
      fieldA: 'a',
      fieldB: 'b',
      result: rawJson,
      durationMs: 12,
      executedAt: '2026-09-14T12:00:00Z',
      recentExecutions: [],
      monthlyExecutionCount: 1
    });

    component.setViewMode('raw');
    expect(component.viewMode()).toBe('raw');
    expect(component.showRawJson()).toBeTrue();

    component.setViewMode('formatted');
    expect(component.viewMode()).toBe('formatted');
    expect(component.showRawJson()).toBeFalse();

    component.setViewMode('visual');
    expect(component.viewMode()).toBe('visual');
  });
});

