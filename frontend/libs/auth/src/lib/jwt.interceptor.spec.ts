import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { jwtInterceptor } from './jwt.interceptor';

describe('jwtInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let authServiceStub: { getAccessToken: () => string | null };

  beforeEach(() => {
    authServiceStub = { getAccessToken: () => null };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([jwtInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authServiceStub },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('adds an Authorization header when a token is present', () => {
    authServiceStub.getAccessToken = () => 'my-token';

    http.get('/health').subscribe();

    const req = httpMock.expectOne('/health');
    expect(req.request.headers.get('Authorization')).toBe('Bearer my-token');
    req.flush({});
  });

  it('leaves the request unchanged when no token is present', () => {
    http.get('/health').subscribe();

    const req = httpMock.expectOne('/health');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });
});
