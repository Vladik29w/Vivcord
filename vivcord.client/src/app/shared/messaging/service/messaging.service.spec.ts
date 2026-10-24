import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { MessagingService } from './messaging.service';
import { Injectable } from '@angular/core';
import { environment } from '@environments/environment';

@Injectable({
  providedIn: 'root',
})
class TestMessagingService extends MessagingService {}

describe('MessagingService', () => {
  let service: TestMessagingService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(TestMessagingService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('loadChatHistory should load history and preserve snowflake string IDs', () => {
    const mockSnowflakeConvId = '184729184719283712';
    const mockSnowflakeMsgId = '9223372036854775807';

    service.loadChatHistory(mockSnowflakeConvId).subscribe(messages => {
      expect(messages.length).toBe(1);
      expect(messages[0].id).toBe(mockSnowflakeMsgId);
      expect(typeof messages[0].id).toBe('string');
      expect(messages[0].conversationId).toBe(mockSnowflakeConvId);
      expect(typeof messages[0].conversationId).toBe('string');
      expect(messages[0].text).toBe('Hello Snowflake');
      expect(messages[0].status).toBe('sent');
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/Messaging/history/${mockSnowflakeConvId}`);
    expect(req.request.method).toBe('GET');

    req.flush([
      {
        id: mockSnowflakeMsgId,
        conversationId: mockSnowflakeConvId,
        senderId: 'user-1',
        text: 'Hello Snowflake',
        sentAt: '2026-10-20T12:00:00Z',
      },
    ]);
  });
});
