import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CourseDto, Paged, ProfessorDto, ProgramDto } from '../models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  programs(page = 1, pageSize = 100): Observable<Paged<ProgramDto>> {
    return this.http.get<Paged<ProgramDto>>(`${this.base}/programs`, { params: { page, pageSize } });
  }

  professors(page = 1, pageSize = 100): Observable<Paged<ProfessorDto>> {
    return this.http.get<Paged<ProfessorDto>>(`${this.base}/professors`, { params: { page, pageSize } });
  }

  courses(page = 1, pageSize = 100): Observable<Paged<CourseDto>> {
    return this.http.get<Paged<CourseDto>>(`${this.base}/courses`, { params: { page, pageSize } });
  }
}
