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

  get isExternalApi(): boolean {
    return (this.newOp.category || '').toLowerCase() === 'externalapi';
  }

  get rulePlaceholder(): string {
    if (this.isExternalApi) {
      return 'https://api.agify.io/?name={A}&country_id={B} or https://catfact.ninja/fact?max_length={A}';
    }
    return 'e.g. A * (1 - (B / 100)) or {A} - {B}';
  }

  get ruleHintText(): string {
    if (this.isExternalApi) {
      return 'Use {A} and {B} for dynamic URL parameters (e.g. https://api.agify.io/?name={A}&country_id={B})';
    }
    return 'Use A and B for variables (e.g. A * (1 - (B / 100)) or sqrt(A*A + B*B))';
  }

  get fieldAPlaceholder(): string {
    if (this.isExternalApi) {
      return 'e.g. Query Parameter {A} (e.g. Coin ID / Name / Latitude)';
    }
    return 'e.g. Original Price';
  }

  get fieldBPlaceholder(): string {
    if (this.isExternalApi) {
      return 'e.g. Query Parameter {B} (e.g. Currency / Country Code / Longitude)';
    }
    return 'e.g. Discount Percentage';
  }

  onSubmit(): void {
    if (!this.newOp.key || !this.newOp.displayName) {
      this.errorMessage.set('Please provide both Operation Key and Display Name.');
      return;
    }

    const category = this.newOp.category || 'Dynamic';
    const ruleTemplate = (this.newOp.ruleTemplate || '').trim();

    if (category.toLowerCase() === 'externalapi') {
      if (!ruleTemplate) {
        this.errorMessage.set('External API operations require a valid URL rule template.');
        return;
      }

      if (!ruleTemplate.startsWith('http://') && !ruleTemplate.startsWith('https://')) {
        this.errorMessage.set('External API rule template must start with http:// or https:// (use {A} and {B} as placeholders).');
        return;
      }
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const dto: CreateOperationDto = {
      key: this.newOp.key.trim().toLowerCase(),
      displayName: this.newOp.displayName.trim(),
      category: category,
      ruleTemplate: ruleTemplate,
      fieldAPrompt: (this.newOp.fieldAPrompt || '').trim() || (this.isExternalApi ? 'Parameter A' : 'Field A'),
      fieldBPrompt: (this.newOp.fieldBPrompt || '').trim() || (this.isExternalApi ? 'Parameter B' : 'Field B'),
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

  get resolvedRulePreview(): string {
    const tmpl = (this.newOp.ruleTemplate || '').trim();
    if (!this.isExternalApi || !tmpl) return '';
    const sampleA = (this.newOp.fieldAPrompt || 'sampleA').split(' ')[0].replace(/[^a-zA-Z0-9]/g, '') || 'sampleA';
    const sampleB = (this.newOp.fieldBPrompt || 'sampleB').split(' ')[0].replace(/[^a-zA-Z0-9]/g, '') || 'sampleB';
    return tmpl.replace(/{A}/gi, encodeURIComponent(sampleA)).replace(/{B}/gi, encodeURIComponent(sampleB));
  }

  fillExample(type: 'discount' | 'celsius' | 'greeting' | 'hypotenuse' | 'weather' | 'crypto' | 'catfact' | 'agify' | 'exchange' | 'country' | 'github'): void {
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
    } else if (type === 'weather') {
      this.newOp = {
        key: 'open-meteo-forecast',
        displayName: 'Live Weather Forecast API',
        category: 'ExternalApi',
        ruleTemplate: 'https://api.open-meteo.com/v1/forecast?latitude={A}&longitude={B}&current=temperature_2m,relative_humidity_2m,wind_speed_10m',
        fieldAPrompt: 'Latitude (קו רוחב)',
        fieldBPrompt: 'Longitude (קו אורך)',
        description: 'Fetches real-time temperature, humidity, and wind speed from Open-Meteo REST API',
        isActive: true
      };
    } else if (type === 'crypto') {
      this.newOp = {
        key: 'crypto-price',
        displayName: 'Crypto Live Price (CoinGecko)',
        category: 'ExternalApi',
        ruleTemplate: 'https://api.coingecko.com/api/v3/simple/price?ids={A}&vs_currencies={B}',
        fieldAPrompt: 'Coin ID (e.g. bitcoin, ethereum)',
        fieldBPrompt: 'Target Currency (e.g. usd, ils, eur)',
        description: 'Fetches real-time crypto asset market prices via CoinGecko REST API',
        isActive: true
      };
    } else if (type === 'catfact') {
      this.newOp = {
        key: 'cat-fact',
        displayName: 'Random Cat Fact API',
        category: 'ExternalApi',
        ruleTemplate: 'https://catfact.ninja/fact?max_length={A}',
        fieldAPrompt: 'Max Length (Characters)',
        fieldBPrompt: 'Unused (Enter 0)',
        description: 'Retrieves a random cat fact within specified character limit',
        isActive: true
      };
    } else if (type === 'agify') {
      this.newOp = {
        key: 'predict-age',
        displayName: 'Name Age Predictor (Agify)',
        category: 'ExternalApi',
        ruleTemplate: 'https://api.agify.io/?name={A}&country_id={B}',
        fieldAPrompt: 'First Name (e.g. michael)',
        fieldBPrompt: 'Country Code (e.g. IL, US)',
        description: 'Predicts demographic age based on given name and country code',
        isActive: true
      };
    } else if (type === 'exchange') {
      this.newOp = {
        key: 'exchange-rate',
        displayName: 'Forex Exchange Rates API',
        category: 'ExternalApi',
        ruleTemplate: 'https://open.er-api.com/v6/latest/{A}',
        fieldAPrompt: 'Base Currency Code (e.g. USD, EUR, ILS)',
        fieldBPrompt: 'Unused (Enter 0)',
        description: 'Fetches real-time international foreign exchange rates against base currency',
        isActive: true
      };
    } else if (type === 'country') {
      this.newOp = {
        key: 'country-info',
        displayName: 'Country Details API (REST Countries)',
        category: 'ExternalApi',
        ruleTemplate: 'https://restcountries.com/v3.1/name/{A}',
        fieldAPrompt: 'Country Name (e.g. Israel, United States)',
        fieldBPrompt: 'Unused (Enter 0)',
        description: 'Fetches population, capital city, region, and subregion for given country',
        isActive: true
      };
    } else if (type === 'github') {
      this.newOp = {
        key: 'github-user',
        displayName: 'GitHub Profile Lookup API',
        category: 'ExternalApi',
        ruleTemplate: 'https://api.github.com/users/{A}',
        fieldAPrompt: 'GitHub Username (e.g. angular, dotnet)',
        fieldBPrompt: 'Unused (Enter 0)',
        description: 'Retrieves public user profile info, repository count, and follower count from GitHub',
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
