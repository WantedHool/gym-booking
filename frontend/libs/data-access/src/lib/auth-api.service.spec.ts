import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthApiService } from './auth-api.service';

describe('AuthApiService', () => {
  let service: AuthApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('posts credentials to /auth/login', () => {
    service
      .login({ email: 'a@b.com', password: 'pw' })
      .subscribe((response) => {
        expect(response.accessToken).toBe('token-123');
      });

    const req = httpMock.expectOne('/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'a@b.com', password: 'pw' });
    req.flush({ accessToken: 'token-123' });
  });

  it('posts registration details to /auth/register with the invite token as a query param', () => {
    service
      .register('raw-token', { password: 'pw', firstName: 'Ana', lastName: 'Doe' })
      .subscribe();

    const req = httpMock.expectOne('/auth/register?token=raw-token');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ password: 'pw', firstName: 'Ana', lastName: 'Doe' });
    req.flush(null);
  });
});
