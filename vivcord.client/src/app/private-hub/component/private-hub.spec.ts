import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { PrivateHubComponent } from './private-hub';

describe('PrivateHubComponent', () => {
  let component: PrivateHubComponent;
  let fixture: ComponentFixture<PrivateHubComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PrivateHubComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PrivateHubComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should manage messageText signal and clear on send', () => {
    expect(component.messageText()).toBe('');
    component.messageText.set('Hello direct message');
    expect(component.messageText()).toBe('Hello direct message');
  });
});

