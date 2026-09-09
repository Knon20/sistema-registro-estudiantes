import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Paged, StudentDto } from '../models';
import { environment } from '../../../environments/environment';

export interface CreateStudentPayload {
  fullName: string;
  email: string;
  documentId: string;
  programId: string;
}

export interface UpdateStudentPayload {
  fullName: string;
  email: string;
  programId: string;
}

@Injectable({ providedIn: 'root' })
export class StudentService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/students`;

  list(page = 1, pageSize = 20): Observable<Paged<StudentDto>> {
    return this.http.get<Paged<StudentDto>>(this.base, { params: { page, pageSize } });
  }

  get(id: string): Observable<StudentDto> {
    return this.http.get<StudentDto>(`${this.base}/${id}`);
  }

  create(payload: CreateStudentPayload): Observable<StudentDto> {
    return this.http.post<StudentDto>(this.base, payload);
  }

  update(id: string, payload: UpdateStudentPayload): Observable<StudentDto> {
    return this.http.put<StudentDto>(`${this.base}/${id}`, payload);
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
