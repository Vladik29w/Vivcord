import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { HomeComponent } from './home-component';

describe('HomeComponent', () => {
  let component: HomeComponent;
  let fixture: ComponentFixture<HomeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HomeComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with default sidebar width', () => {
    expect(component.sidebarWidth()).toBe(300);
    expect(component.isResizing()).toBe(false);
  });

  it('should render the resizer handle in DOM', () => {
    const resizerEl = fixture.nativeElement.querySelector('.sidebar-resizer');
    expect(resizerEl).toBeTruthy();
    expect(resizerEl.getAttribute('role')).toBe('separator');
  });

  it('should reset sidebar width when resetWidth is called', () => {
    component.sidebarWidth.set(450);
    expect(component.sidebarWidth()).toBe(450);
    component.resetWidth();
    expect(component.sidebarWidth()).toBe(300);
  });
});

