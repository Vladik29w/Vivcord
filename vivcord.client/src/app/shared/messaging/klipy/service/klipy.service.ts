import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@environments/environment';
import { GifPagedResult } from '../dto/klipy.dto';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class Klipy {
  private readonly http = inject(HttpClient);

  getTrendingGifs(): Observable<GifPagedResult> {
    return this.http.get<GifPagedResult>(`${environment.apiUrl}/Klipy/trending`);
  }

  searchGifs(query: string): Observable<GifPagedResult> {
    return this.http.get<GifPagedResult>(`${environment.apiUrl}/Klipy/search?query=${query}`);
  }
}
