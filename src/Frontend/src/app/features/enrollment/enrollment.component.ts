import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CatalogService } from '../../core/services/catalog.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { StudentService } from '../../core/services/student.service';
import { CourseDto, EnrollmentDto } from '../../core/models';
import { apiMessage } from '../../core/http/api-error';

@Component({
  selector: 'app-enrollment',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section class="card">
      <h2>Inscripción · {{ studentName() }}</h2>
      <p class="muted">Selecciona exactamente <strong>3 materias</strong> de profesores diferentes. Cada materia vale 3 créditos (total 9).</p>

      <p *ngIf="error()" class="error">{{ error() }}</p>
      <p *ngIf="success()" class="success">{{ success() }}</p>

      <div class="grid">
        <label *ngFor="let c of courses()" class="course" [class.conflict]="isConflict(c)" [class.picked]="isSelected(c.id)">
          <input type="checkbox" [checked]="isSelected(c.id)" (change)="toggle(c)" [disabled]="!isSelected(c.id) && selectedIds().length >= 3" />
          <div>
            <strong>{{ c.code }} — {{ c.name }}</strong>
            <div class="muted">{{ c.professorName }} · {{ c.credits }} créditos</div>
          </div>
        </label>
      </div>

      <div class="summary">
        <span class="chip">Seleccionadas: <strong>{{ selectedIds().length }}/3</strong></span>
        <span class="chip">Créditos: <strong>{{ totalCredits() }}/9</strong></span>
        <span class="chip">Profesores: <strong>{{ professorCount() }}</strong></span>
      </div>

      <p *ngIf="conflictMessage()" class="error">{{ conflictMessage() }}</p>

      <div class="row">
        <button class="btn primary" (click)="save()" [disabled]="!canSave() || saving()">{{ hasExisting() ? 'Actualizar inscripción' : 'Crear inscripción' }}</button>
        <a [routerLink]="studentLink()" class="btn">Volver</a>
        <a routerLink="/students" class="btn">Inicio</a>
        <label class="period">Periodo <input [(ngModel)]="period" /></label>
      </div>

      <div *ngIf="current()" class="current">
        <h3>Inscripción actual ({{ current()?.period }})</h3>
        <ul>
          <li *ngFor="let c of current()?.courses">{{ c.code }} — {{ c.name }} ({{ c.professorName }})</li>
        </ul>
      </div>
    </section>
  `,
  styles: [`
    .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(min(100%, 260px), 1fr)); gap: .7rem; margin: 1rem 0; }
    .course {
      display: flex; gap: .65rem; align-items: flex-start;
      border: 1px solid var(--border); border-radius: 14px; padding: .8rem;
      cursor: pointer; background: var(--surface-2); transition: all .18s ease;
      font-weight: 400;
    }
    .course:hover { transform: translateY(-2px); box-shadow: var(--shadow); }
    .course input { width: auto; accent-color: var(--accent); margin-top: .2rem; }
    .course.picked { border-color: var(--accent); box-shadow: var(--glow); }
    .course.conflict { border-color: var(--danger-border); background: var(--danger-bg); }
    .summary { display: flex; gap: .6rem; margin: .8rem 0; flex-wrap: wrap; }
    .chip {
      padding: .4rem .9rem; border-radius: 999px; font-size: .85rem;
      background: var(--primary-soft); border: 1px solid var(--border); color: var(--text);
    }
    .row { display: flex; gap: .6rem; align-items: center; margin-top: .8rem; flex-wrap: wrap; }
    .period { margin-left: auto; display: flex; gap: .4rem; align-items: center; }
    .period input { width: 96px; }
    .current { margin-top: 1rem; border-top: 1px solid var(--border); padding-top: .8rem; }
    .current ul { display: grid; gap: .4rem; padding-left: 1.1rem; }
    @media (max-width: 640px) {
      .period { margin-left: 0; width: 100%; }
      .period input { flex: 1; }
      .row .btn { flex: 1; justify-content: center; }
    }
  `]
})
export class EnrollmentComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private catalog = inject(CatalogService);
  private enrollments = inject(EnrollmentService);
  private students = inject(StudentService);
  protected studentLink(): string[] {
    return this.studentId ? ['/students', this.studentId] : ['/students'];
  }

  courses = signal<CourseDto[]>([]);
  selectedIds = signal<string[]>([]);
  error = signal<string | null>(null);
  success = signal<string | null>(null);
  saving = signal(false);
  studentName = signal('');
  current = signal<EnrollmentDto | null>(null);
  period = '2026-1';
  private studentId = '';

  totalCredits = computed(() =>
    this.courses().filter(c => this.selectedIds().includes(c.id)).reduce((a, c) => a + c.credits, 0));

  professorCount = computed(() =>
    new Set(this.courses().filter(c => this.selectedIds().includes(c.id)).map(c => c.professorId)).size);

  hasExisting = computed(() => this.current() !== null);

  conflictMessage = computed(() => {
    const sel = this.courses().filter(c => this.selectedIds().includes(c.id));
    const profs = sel.map(c => c.professorId);
    if (new Set(profs).size !== profs.length)
      return 'Dos materias seleccionadas pertenecen al mismo profesor. Elige profesores diferentes.';
    return null;
  });

  canSave = computed(() =>
    this.selectedIds().length === 3 && this.totalCredits() === 9 && !this.conflictMessage());

  ngOnInit(): void {
    this.studentId = this.route.snapshot.paramMap.get('studentId') ?? '';
    this.catalog.courses().subscribe({ next: (res) => this.courses.set(res.items) });
    this.students.get(this.studentId).subscribe({ next: (s) => this.studentName.set(s.fullName) });
    this.load();
  }

  load(): void {
    this.enrollments.get(this.studentId, this.period).subscribe({
      next: (e) => { this.current.set(e); this.selectedIds.set(e.courses.map(c => c.id)); },
      error: () => this.current.set(null)
    });
  }

  isSelected(id: string): boolean { return this.selectedIds().includes(id); }

  isConflict(course: CourseDto): boolean {
    if (!this.isSelected(course.id)) return false;
    const sel = this.courses().filter(c => this.isSelected(c.id) && c.id !== course.id);
    return sel.some(c => c.professorId === course.professorId);
  }

  toggle(course: CourseDto): void {
    const ids = [...this.selectedIds()];
    const i = ids.indexOf(course.id);
    if (i >= 0) ids.splice(i, 1);
    else { if (ids.length >= 3) return; ids.push(course.id); }
    this.selectedIds.set(ids);
    this.error.set(null);
    this.success.set(null);
  }

  save(): void {
    if (!this.canSave()) return;
    this.saving.set(true);
    this.error.set(null);
    const ids = this.selectedIds();
    const done = {
      next: (e: EnrollmentDto) => { this.current.set(e); this.success.set('Inscripción guardada correctamente.'); this.saving.set(false); },
      error: (e: unknown) => { this.error.set(apiMessage(e, 'No se pudo guardar la inscripción.')); this.saving.set(false); }
    };
    if (this.hasExisting()) this.enrollments.update(this.studentId, this.period, ids).subscribe(done);
    else this.enrollments.create(this.studentId, this.period, ids).subscribe(done);
  }
}
