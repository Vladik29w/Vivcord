import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { GroupHubComponent } from './group-hub';

describe('GroupHubComponent', () => {
  let component: GroupHubComponent;
  let fixture: ComponentFixture<GroupHubComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GroupHubComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(GroupHubComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should manage messageText signal and clear on send', () => {
    expect(component.messageText()).toBe('');
    component.messageText.set('Hello world');
    expect(component.messageText()).toBe('Hello world');
  });

  it('should manage inviteNickname signal', () => {
    expect(component.inviteNickname()).toBe('');
    component.inviteNickname.set('testuser');
    expect(component.inviteNickname()).toBe('testuser');
  });
});
