import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Document, DocumentListResponse, DocumentUploadResponse } from '../models/document.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class DocumentService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/documents`;

  getDocuments(page: number = 1, pageSize: number = 10): Observable<DocumentListResponse> {
    return this.http.get<DocumentListResponse>(`${this.apiUrl}?page=${page}&pageSize=${pageSize}`);
  }

  getDocument(id: string): Observable<Document> {
    return this.http.get<Document>(`${this.apiUrl}/${id}`);
  }

  uploadDocument(file: File, userId?: string): Observable<DocumentUploadResponse> {
    const formData = new FormData();
    formData.append('file', file);

    const headers = new HttpHeaders();
    if (userId) {
      headers.set('X-User-Id', userId);
    }

    return this.http.post<DocumentUploadResponse>(this.apiUrl, formData, { headers });
  }

  getDownloadUrl(id: string): Observable<{ downloadUrl: string }> {
    return this.http.get<{ downloadUrl: string }>(`${this.apiUrl}/${id}/download`);
  }

  deleteDocument(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
