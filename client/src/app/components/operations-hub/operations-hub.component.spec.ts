import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { OperationsHubComponent } from './operations-hub.component';
import { OperationsService } from '../../services/operations.service';
import { OperationDto, CreateOperationDto } from '../../models/operation.model';

describe('OperationsHubComponent', () => {
  let component: OperationsHubComponent;
  let fixture: ComponentFixture<OperationsHubComponent>;
  let mockOperationsService: jasmine.SpyObj<OperationsService>;
  let router: Router;

  const mockOps: OperationDto[] = [
    {
      key: 'add',
      displayName: 'Addition',
      category: 'Arithmetic',
      ruleTemplate: 'a + b',
      fieldAPrompt: 'A',
      fieldBPrompt: 'B',
      description: 'Adds two numbers',
      isActive: true
    },
    {
      key: 'concat',
      displayName: 'String Concat',
      category: 'String',
      ruleTemplate: 'a + b',
      fieldAPrompt: 'First',
      fieldBPrompt: 'Second',
      description: 'Joins two strings',
      isActive: false
    }
  ];

  beforeEach(async () => {
    mockOperationsService = jasmine.createSpyObj('OperationsService', [
      'getOperations',
      'createOperation',
      'setOperationStatus'
    ]);

    mockOperationsService.getOperations.and.returnValue(of(mockOps));

    await TestBed.configureTestingModule({
      imports: [OperationsHubComponent],
      providers: [
        provideRouter([]),
        { provide: OperationsService, useValue: mockOperationsService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(OperationsHubComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create and load registered operations', () => {
    expect(component).toBeTruthy();
    expect(mockOperationsService.getOperations).toHaveBeenCalled();
    expect(component.operations().length).toBe(2);
    expect(component.filteredOperations().length).toBe(2);
  });

  it('should filter operations by search term and category', () => {
    component.searchTerm.set('concat');
    expect(component.filteredOperations().length).toBe(1);
    expect(component.filteredOperations()[0].key).toBe('concat');

    component.searchTerm.set('');
    component.selectedCategory.set('Arithmetic');
    expect(component.filteredOperations().length).toBe(1);
    expect(component.filteredOperations()[0].key).toBe('add');
  });

  it('should create new operation and refresh operations list', () => {
    const newOpDto: CreateOperationDto = {
      key: 'hypotenuse',
      displayName: 'Hypotenuse',
      category: 'Dynamic',
      ruleTemplate: 'sqrt(A*A + B*B)',
      fieldAPrompt: 'Base',
      fieldBPrompt: 'Height',
      description: 'Calculates hypotenuse',
      isActive: true
    };

    const createdResult: OperationDto = {
      ...newOpDto,
      isActive: true
    };

    mockOperationsService.createOperation.and.returnValue(of(createdResult));

    component.newOp = { ...newOpDto };
    component.onSubmit();

    expect(mockOperationsService.createOperation).toHaveBeenCalled();
    expect(component.successMessage()).toContain('hypotenuse');
  });

  it('should toggle operation active status', () => {
    mockOperationsService.setOperationStatus.and.returnValue(of(void 0));

    component.toggleStatus(mockOps[0]);

    expect(mockOperationsService.setOperationStatus).toHaveBeenCalledWith('add', false);
  });

  it('should navigate to calculator with queryParams when testInCalculator is called', () => {
    const navigateSpy = spyOn(router, 'navigate');

    component.testInCalculator('add');

    expect(navigateSpy).toHaveBeenCalledWith(['/calculator'], { queryParams: { key: 'add' } });
  });
});
