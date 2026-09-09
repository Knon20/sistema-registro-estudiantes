import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CatalogService } from '../../core/services/catalog.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { CourseDto, ClassmateDto } from '../../core/models';

@Component({
  selector: 'app-classmates',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="card">
      <h2>Compañeros por materia</h2>
      <p class="muted">Solo se muestran los <strong>nombres</strong> de los compañeros (sin datos sensibles).</p>
      <label>Materia
        <select [(ngModel)]="selectedCourse" (ngModelChange)="load()">
          <option value="">Seleccione...</option>
          <option *ngFor="let c of courses()" [value]="c.id">{{ c.code }} — {{ c.name }} ({{ c.professorName }})</option>
        </select>
      </label>
      <p *ngIf="loading()">Cargando...</p>
      <ul *ngIf="!loading() && classmates().length">
        <li *ngFor="let m of classmates()">{{ m.fullName }}</li>
      </ul>
      <p *ngIf="!loading() && selectedCourse && !classmates().length" class="muted">Aún no hay estudiantes inscritos en esta materia.</p>
    </section>
  `,
  styles: [`
    .card { background: #fff; padding: 1.5rem; border-radius: 12px; box-shadow: 0 2px 12px rgba(0,0,0,.06); max-width: 640px; }
    .muted { color: #666; }
    label { display: grid; gap: .4rem; margin: .8rem 0; font-weight: 600; }
    select { padding: .6rem; border-radius: 8px; border: 1px solid #ccc; font-weight: 400; }
    ul { margin-top: .8rem; display: grid; gap: .4rem; }
    li { background: #f6f8fc; padding: .5rem .8rem; border-radius: 8px; }
  `]
})
export class ClassmatesComponent implements OnInit {
  private catalog = inject(CatalogService);
  private enrollments = inject(EnrollmentService);

  courses = signal<CourseDto[]>([]);
  classmates = signal<ClassmateDto[]>([]);
  selectedCourse = '';
  loading = signal(false);

  ngOnInit(): void {
    this.catalog.courses().subscribe({ next: (c) => this.courses.set(c) });
  }

  load(): void {
    if (!this.selectedCourse) { this.classmates.set([]); return; }
    this.loading.set(true);
    this.enrollments.classmates(this.selectedCourse).subscribe({
      next: (items) => { this.classmates.set(items); this.loading.set(false); },
      error: () => { this.classmates.set([]); this.loading.set(false); }
    });
  }
}
