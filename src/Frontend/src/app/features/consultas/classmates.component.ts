import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CatalogService } from '../../core/services/catalog.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { CourseDto, ClassmateDto } from '../../core/models';
import { apiMessage } from '../../core/http/api-error';

@Component({
  selector: 'app-classmates',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="card mates-card">
      <h2>Compañeros por materia · {{ total() }}</h2>
      <p class="muted">Solo se muestran los <strong>nombres</strong> de los compañeros (sin datos sensibles).</p>
      <p *ngIf="error()" class="error">{{ error() }}</p>
      <label>Materia
        <select [(ngModel)]="selectedCourse" (ngModelChange)="resetAndLoad()">
          <option value="">Seleccione...</option>
          <option *ngFor="let c of courses()" [value]="c.id">{{ c.code }} — {{ c.name }} ({{ c.professorName }})</option>
        </select>
      </label>
      <p *ngIf="loading()" class="muted">Cargando...</p>
      <ul *ngIf="!loading() && classmates().length" class="mates">
        <li *ngFor="let m of classmates()"><span class="avatar">{{ m.fullName.charAt(0) }}</span>{{ m.fullName }}</li>
      </ul>
      <p *ngIf="!loading() && selectedCourse && !classmates().length" class="muted">Aún no hay estudiantes inscritos en esta materia.</p>
      <div class="pager" *ngIf="total() > pageSize">
        <button class="btn" (click)="prev()" [disabled]="page() <= 1">← Anterior</button>
        <span class="muted">Página {{ page() }} de {{ totalPages() }}</span>
        <button class="btn" (click)="next()" [disabled]="page() >= totalPages()">Siguiente →</button>
      </div>
    </section>
  `,
  styles: [`
    .mates-card { max-width: 680px; margin-inline: auto; }
    label { display: grid; gap: .4rem; margin: .8rem 0; }
    .mates { margin-top: .8rem; display: grid; gap: .5rem; padding: 0; list-style: none; }
    .mates li {
      display: flex; align-items: center; gap: .7rem;
      background: var(--surface-2); border: 1px solid var(--border);
      padding: .55rem .8rem; border-radius: 12px;
    }
    .avatar {
      display: grid; place-items: center; width: 30px; height: 30px; border-radius: 50%;
      font-weight: 800; font-size: .85rem; color: #fff; flex-shrink: 0;
      background: linear-gradient(135deg, var(--primary), var(--accent));
    }
  `]
})
export class ClassmatesComponent implements OnInit {
  private catalog = inject(CatalogService);
  private enrollments = inject(EnrollmentService);

  courses = signal<CourseDto[]>([]);
  classmates = signal<ClassmateDto[]>([]);
  total = signal(0);
  page = signal(1);
  readonly pageSize = 20;
  selectedCourse = '';
  loading = signal(false);
  error = signal<string | null>(null);

  totalPages(): number {
    return Math.max(1, Math.ceil(this.total() / this.pageSize));
  }

  ngOnInit(): void {
    this.catalog.courses().subscribe({ next: (res) => this.courses.set(res.items) });
  }

  resetAndLoad(): void {
    this.page.set(1);
    this.load();
  }

  prev(): void {
    if (this.page() > 1) { this.page.set(this.page() - 1); this.load(); }
  }

  next(): void {
    if (this.page() < this.totalPages()) { this.page.set(this.page() + 1); this.load(); }
  }

  load(): void {
    if (!this.selectedCourse) { this.classmates.set([]); this.total.set(0); return; }
    this.loading.set(true);
    this.error.set(null);
    this.enrollments.classmates(this.selectedCourse, this.page(), this.pageSize).subscribe({
      next: (res) => { this.classmates.set(res.items); this.total.set(res.total); this.loading.set(false); },
      error: (e) => { this.error.set(apiMessage(e, 'No se pudo cargar los compañeros.')); this.classmates.set([]); this.loading.set(false); }
    });
  }
}
