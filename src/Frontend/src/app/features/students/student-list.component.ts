import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { StudentService } from '../../core/services/student.service';
import { StudentDto, ApiError } from '../../core/models';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-student-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="card">
      <header class="card-head">
        <h2>Estudiantes</h2>
        <a routerLink="/students/new" class="btn primary">Nuevo estudiante</a>
      </header>

      <p *ngIf="error()" class="error">{{ error() }}</p>
      <p *ngIf="loading()">Cargando...</p>

      <table *ngIf="!loading() && students().length">
        <thead>
          <tr><th>Nombre</th><th>Email</th><th>Documento</th><th>Programa</th><th></th></tr>
        </thead>
        <tbody>
          <tr *ngFor="let s of students()">
            <td><a [routerLink]="['/students', s.id]">{{ s.fullName }}</a></td>
            <td>{{ s.email }}</td>
            <td>{{ s.documentId }}</td>
            <td>{{ s.programName ?? s.programId }}</td>
            <td class="actions">
              <a [routerLink]="['/students', s.id, 'edit']" class="btn">Editar</a>
              <a [routerLink]="['/enrollment', s.id]" class="btn">Inscripción</a>
              <button class="btn danger" (click)="remove(s)">Eliminar</button>
            </td>
          </tr>
        </tbody>
      </table>
      <p *ngIf="!loading() && !students().length">No hay estudiantes registrados.</p>
    </section>
  `,
  styles: [`
    .card { background: #fff; padding: 1.5rem; border-radius: 12px; box-shadow: 0 2px 12px rgba(0,0,0,.06); }
    .card-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
    table { width: 100%; border-collapse: collapse; }
    th, td { text-align: left; padding: .6rem; border-bottom: 1px solid #eee; }
    .actions { display: flex; gap: .5rem; }
    .btn { padding: .4rem .8rem; border-radius: 8px; border: 1px solid #ddd; background: #f8f8f8; cursor: pointer; text-decoration: none; color: #333; font-size: .85rem; }
    .btn.primary { background: #1a73e8; color: #fff; border-color: #1a73e8; }
    .btn.danger { background: #fdecea; border-color: #f5c6cb; color: #b3261e; }
    .error { color: #b3261e; background: #fdecea; padding: .6rem; border-radius: 8px; }
  `]
})
export class StudentListComponent implements OnInit {
  private api = inject(StudentService);
  students = signal<StudentDto[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    this.api.list().subscribe({
      next: (items) => { this.students.set(items); this.loading.set(false); },
      error: (e: HttpErrorResponse) => { this.error.set((e.error as ApiError)?.message ?? 'No se pudo cargar el listado.'); this.loading.set(false); }
    });
  }

  remove(s: StudentDto): void {
    if (!confirm(`¿Eliminar a ${s.fullName}? Se eliminará también su inscripción.`)) return;
    this.api.remove(s.id).subscribe({
      next: () => this.students.set(this.students().filter(x => x.id !== s.id)),
      error: (e: HttpErrorResponse) => alert((e.error as ApiError)?.message ?? 'No se pudo eliminar.')
    });
  }
}
