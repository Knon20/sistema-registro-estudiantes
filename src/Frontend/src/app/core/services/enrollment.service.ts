import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ClassmateDto, EnrollmentDto, Paged } from '../models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class EnrollmentService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/enrollments`;

  create(studentId: string, period: string, courseIds: string[]): Observable<EnrollmentDto> {
    return this.http.post<EnrollmentDto>(this.base, { studentId, period, courseIds });
  }

  get(studentId: string, period = '2026-1'): Observable<EnrollmentDto> {
    return this.http.get<EnrollmentDto>(`${this.base}/student/${studentId}`, { params: { period } });
  }

  update(studentId: string, period: string, courseIds: string[]): Observable<EnrollmentDto> {
    return this.http.put<EnrollmentDto>(`${this.base}/student/${studentId}`, { courseIds }, { params: { period } });
  }

  classmates(courseId: string, page = 1, pageSize = 20): Observable<Paged<ClassmateDto>> {
    return this.http.get<Paged<ClassmateDto>>(`${this.base}/courses/${courseId}/classmates`, { params: { page, pageSize } });
  }
}
