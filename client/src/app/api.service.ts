// client/src/app/api.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface StatsResponse {
  message: string;
  total_page_loads: number;
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private http = inject(HttpClient);
  
  // Explicitly point to port 5000 where Docker maps your backend API
  private baseUrl = 'http://localhost:5000';

  getStats(): Observable<StatsResponse> {
    return this.http.get<StatsResponse>(`${this.baseUrl}/api/stats`);
  }

  addUser(name: string): Observable<string> {
    const formData = new FormData();
    formData.append('name', name);
    return this.http.post(`${this.baseUrl}/add-user`, formData, { responseType: 'text' });
  }
}