import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { StudentService } from '../../core/services/student.service';
import { ConfirmDialogService } from '../../shared/confirm-dialog/confirm-dialog.service';
import { StudentDto } from '../../core/models';
import { apiMessage } from '../../core/http/api-error';

@Component({
  selector: 'app-student-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="card">
      <header class="card-head">
        <h2>Estudiantes · {{ total() }}</h2>
        <a routerLink="/students/new" class="btn primary">+ Nuevo estudiante</a>
      </header>

      <p *ngIf="error()" class="error">{{ error() }}</p>
      <p *ngIf="loading()" class="muted">Cargando...</p>

      <div class="table-wrap" *ngIf="!loading() && students().length">
        <table>
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
      </div>
      <p *ngIf="!loading() && !students().length" class="muted">No hay estudiantes registrados.</p>

      <div class="pager" *ngIf="total() > pageSize">
        <button class="btn" (click)="prev()" [disabled]="page() <= 1">← Anterior</button>
        <span class="muted">Página {{ page() }} de {{ totalPages() }}</span>
        <button class="btn" (click)="next()" [disabled]="page() >= totalPages()">Siguiente →</button>
      </div>
    </section>
  `,
  styles: [`
    .card-head { display: flex; justify-content: space-between; align-items: center; gap: 1rem; margin-bottom: 1rem; flex-wrap: wrap; }
    .actions { display: flex; gap: .45rem; flex-wrap: wrap; }
    @media (max-width: 640px) {
      .card-head { flex-direction: column; align-items: stretch; }
      .card-head .btn { justify-content: center; }
    }
  `]
})
export class StudentListComponent implements OnInit {
  private api = inject(StudentService);
  private dialog = inject(ConfirmDialogService);
  students = signal<StudentDto[]>([]);
  total = signal(0);
  page = signal(1);
  readonly pageSize = 10;
  loading = signal(true);
  error = signal<string | null>(null);

  totalPages(): number {
    return Math.max(1, Math.ceil(this.total() / this.pageSize));
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.list(this.page(), this.pageSize).subscribe({
      next: (res) => { this.students.set(res.items); this.total.set(res.total); this.loading.set(false); },
      error: (e) => { this.error.set(apiMessage(e, 'No se pudo cargar el listado.')); this.loading.set(false); }
    });
  }

  prev(): void {
    if (this.page() > 1) { this.page.set(this.page() - 1); this.load(); }
  }

  next(): void {
    if (this.page() < this.totalPages()) { this.page.set(this.page() + 1); this.load(); }
  }

  async remove(s: StudentDto): Promise<void> {
    const ok = await this.dialog.open({
      title: 'Eliminar estudiante',
      message: `¿Eliminar a ${s.fullName}? También se eliminará su inscripción y no podrás deshacerlo.`,
      confirmText: 'Sí, eliminar',
    });
    if (!ok) return;
    this.api.remove(s.id).subscribe({
      next: () => this.load(),
      error: (e) => alert(apiMessage(e, 'No se pudo eliminar.'))
    });
  }
}
