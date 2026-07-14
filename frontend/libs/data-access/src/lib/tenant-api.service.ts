import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { TenantSettings } from '@frontend/models';

@Injectable({ providedIn: 'root' })
export class TenantApiService {
  private readonly http = inject(HttpClient);

  get(): Observable<TenantSettings> {
    return this.http.get<TenantSettings>('/tenant/settings');
  }

  update(settings: TenantSettings): Observable<void> {
    return this.http.put<void>('/tenant/settings', settings);
  }
}
