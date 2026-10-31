import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { GroupManagementService } from './group-management.service';
import { environment } from '@environments/environment';

describe('GroupManagementService', () => {
  let service: GroupManagementService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(GroupManagementService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getGroup should request conversation by string conversationId from /Conversation/:id', () => {
    const convId = '184729184719283712';
    const mockGroup = {
      id: convId,
      title: 'Test Group',
      type: 'Group' as const,
      createdAt: '2026-10-20T12:00:00Z',
      members: [
        {
          userId: 'admin-guid',
          userName: 'admin',
          displayName: 'Admin User',
          role: 'Admin' as const,
          status: 'Joined' as const,
        },
      ],
    };

    service.getGroup(convId).subscribe(group => {
      expect(group.id).toBe(convId);
      expect(typeof group.id).toBe('string');
      expect(group.title).toBe('Test Group');
      expect(group.name).toBe('Test Group');
      expect(group.adminId).toBe('admin-guid');
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/Conversation/${convId}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockGroup);
  });

  it('inviteMember should POST to /Group/invite/:conversationId', () => {
    const convId = '184729184719283712';
    service.inviteMember(convId, 'alice').subscribe();

    const req = httpTesting.expectOne(`${environment.apiUrl}/Group/invite/${convId}`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ username: 'alice' });
    req.flush(null);
  });

  it('kickMember should DELETE /Group/kick/:conversationId?username=...', () => {
    const convId = '184729184719283712';
    service.kickMember(convId, 'alice').subscribe();

    const req = httpTesting.expectOne(`${environment.apiUrl}/Group/kick/${convId}?username=alice`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
