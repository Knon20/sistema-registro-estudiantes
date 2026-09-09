import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CourseDto, ProfessorDto, ProgramDto } from '../models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  programs(): Observable<ProgramDto[]> {
    return this.http.get<ProgramDto[]>(`${this.base}/programs`);
  }

  professors(): Observable<ProfessorDto[]> {
    return this.http.get<ProfessorDto[]>(`${this.base}/professors`);
  }

  courses(): Observable<CourseDto[]> {
    return this.http.get<CourseDto[]>(`${this.base}/courses`);
  }
}
