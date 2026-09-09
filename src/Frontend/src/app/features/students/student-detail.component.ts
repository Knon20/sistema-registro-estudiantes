import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { StudentService } from '../../core/services/student.service';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { NavigationService } from '../../core/navigation/navigation.service';
import { StudentDto, EnrollmentDto } from '../../core/models';
import { apiMessage } from '../../core/http/api-error';

@Component({
  selector: 'app-student-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="card detail-card">
      <p *ngIf="error()" class="error">{{ error() }}</p>
      <p *ngIf="loading()" class="muted">Cargando...</p>

      <ng-container *ngIf="student() as s">
        <div class="head">
          <span class="avatar">{{ s.fullName.charAt(0) }}</span>
          <div>
            <h2>{{ s.fullName }}</h2>
            <p class="muted">{{ s.email }} · Doc. {{ s.documentId }}</p>
          </div>
        </div>

        <dl>
          <div><dt>Programa</dt><dd>{{ s.programName ?? s.programId }}</dd></div>
          <div><dt>Registro</dt><dd>{{ s.createdAt | date:'mediumDate' }}</dd></div>
          <div><dt>Periodo</dt><dd>{{ period }}</dd></div>
        </dl>

        <h3>Inscripción</h3>
        <ul *ngIf="enrollment() as e" class="courses">
          <li *ngFor="let c of e.courses">
            <strong>{{ c.code }}</strong> — {{ c.name }}
            <span class="muted">({{ c.professorName }} · {{ c.credits }} créditos)</span>
          </li>
        </ul>
        <p *ngIf="!enrollment() && !loading()" class="muted">Sin inscripción en este periodo.</p>
        <p *ngIf="enrollment() as e" class="muted">Total: <strong>{{ e.totalCredits }} créditos</strong></p>

        <div class="row">
          <a [routerLink]="['/students', s.id, 'edit']" class="btn">Editar</a>
          <a [routerLink]="['/enrollment', s.id]" class="btn primary">Inscripción</a>
          <button class="btn" (click)="nav.back()">Volver</button>
        </div>
      </ng-container>
    </section>
  `,
  styles: [`
    .detail-card { max-width: 640px; margin-inline: auto; }
    .head { display: flex; gap: 1rem; align-items: center; margin-bottom: 1rem; }
    .head h2 { margin: 0; }
    .head p { margin: .2rem 0 0; }
    .avatar {
      display: grid; place-items: center; width: 56px; height: 56px; border-radius: 18px;
      font-weight: 800; font-size: 1.4rem; color: #fff; flex-shrink: 0;
      background: linear-gradient(135deg, var(--primary), var(--accent));
      box-shadow: 0 0 20px rgba(79, 70, 229, .45);
    }
    dl { display: grid; gap: .5rem; margin: 0 0 1rem; }
    dl div { display: flex; gap: .8rem; background: var(--surface-2); border: 1px solid var(--border); border-radius: 12px; padding: .55rem .8rem; }
    dt { font-weight: 700; min-width: 90px; }
    dd { margin: 0; }
    .courses { display: grid; gap: .5rem; padding: 0; list-style: none; }
    .courses li { background: var(--surface-2); border: 1px solid var(--border); padding: .55rem .8rem; border-radius: 12px; }
    .row { display: flex; gap: .6rem; margin-top: 1.2rem; flex-wrap: wrap; }
    .row .btn { flex: 1; justify-content: center; min-width: 120px; }
    @media (max-width: 520px) { .head { flex-direction: column; text-align: center; } }
  `]
})
export class StudentDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private students = inject(StudentService);
  private enrollments = inject(EnrollmentService);
  protected nav = inject(NavigationService);

  student = signal<StudentDto | null>(null);
  enrollment = signal<EnrollmentDto | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);
  readonly period = '2026-1';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.students.get(id).subscribe({
      next: (s) => {
        this.student.set(s);
        this.enrollments.get(id, this.period).subscribe({
          next: (e) => { this.enrollment.set(e); this.loading.set(false); },
          error: () => this.loading.set(false),
        });
      },
      error: (e) => { this.error.set(apiMessage(e, 'No se pudo cargar el estudiante.')); this.loading.set(false); },
    });
  }
}
