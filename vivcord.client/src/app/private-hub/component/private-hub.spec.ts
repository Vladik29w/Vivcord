import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PrivateHubService } from '../service/private-hub.service';

describe('PrivateHub', () => {
  let component: PrivateHubService;
  let fixture: ComponentFixture<PrivateHubService>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [PrivateHubService],
    }).compileComponents();

    fixture = TestBed.createComponent(PrivateHubService);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
