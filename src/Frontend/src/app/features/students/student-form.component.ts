import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { StudentService } from '../../core/services/student.service';
import { CatalogService } from '../../core/services/catalog.service';
import { ProgramDto } from '../../core/models';
import { apiMessage } from '../../core/http/api-error';

@Component({
  selector: 'app-student-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <section class="card">
      <h2>{{ isEdit ? 'Editar estudiante' : 'Nuevo estudiante' }}</h2>
      <p *ngIf="error()" class="error">{{ error() }}</p>
      <form [formGroup]="form" (ngSubmit)="save()">
        <label>Nombre completo
          <input formControlName="fullName" placeholder="Ej. Juan Pérez" />
        </label>
        <label>Email
          <input formControlName="email" type="email" placeholder="nombre@uni.edu" />
        </label>
        <label *ngIf="!isEdit">Documento
          <input formControlName="documentId" placeholder="Ej. 1001001" />
        </label>
        <label>Programa
          <select formControlName="programId">
            <option value="">Seleccione...</option>
            <option *ngFor="let p of programs()" [value]="p.id">{{ p.name }} ({{ p.code }})</option>
          </select>
        </label>
        <div class="row">
          <button class="btn primary" type="submit" [disabled]="form.invalid || saving()">Guardar</button>
          <a routerLink="/students" class="btn">Volver</a>
        </div>
      </form>
    </section>
  `,
  styles: [`
    .card { background: #fff; padding: 1.5rem; border-radius: 12px; box-shadow: 0 2px 12px rgba(0,0,0,.06); max-width: 560px; }
    form { display: grid; gap: .9rem; }
    label { display: grid; gap: .3rem; font-weight: 600; }
    input, select { padding: .6rem; border-radius: 8px; border: 1px solid #ccc; font-weight: 400; }
    .row { display: flex; gap: .6rem; }
    .btn { padding: .5rem 1rem; border-radius: 8px; border: 1px solid #ddd; background: #f8f8f8; cursor: pointer; text-decoration: none; color: #333; }
    .btn.primary { background: #1a73e8; color: #fff; border-color: #1a73e8; }
    .btn.primary:disabled { opacity: .6; cursor: not-allowed; }
    .error { color: #b3261e; background: #fdecea; padding: .6rem; border-radius: 8px; }
  `]
})
export class StudentFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private api = inject(StudentService);
  private catalog = inject(CatalogService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  form = this.fb.group({
    fullName: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    documentId: ['', Validators.required],
    programId: ['', Validators.required],
  });

  programs = signal<ProgramDto[]>([]);
  error = signal<string | null>(null);
  saving = signal(false);
  isEdit = false;
  private id: string | null = null;

  ngOnInit(): void {
    this.catalog.programs().subscribe({ next: (res) => this.programs.set(res.items) });
    this.id = this.route.snapshot.paramMap.get('id');
    const url = this.route.snapshot.url.map(s => s.path).join('/');
    if (this.id && url.includes('edit')) {
      this.isEdit = true;
      this.form.get('documentId')?.disable();
      this.api.get(this.id).subscribe({
        next: (s) => this.form.patchValue({ fullName: s.fullName, email: s.email, programId: s.programId }),
        error: (e) => this.error.set(apiMessage(e, 'No se pudo cargar el estudiante.'))
      });
    }
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.error.set(null);
    const v = this.form.getRawValue();

    if (this.isEdit && this.id) {
      this.api.update(this.id, { fullName: v.fullName!, email: v.email!, programId: v.programId! }).subscribe({
        next: () => this.router.navigate(['/students']),
        error: (e) => { this.error.set(apiMessage(e, 'No se pudo guardar.')); this.saving.set(false); }
      });
    } else {
      this.api.create({ fullName: v.fullName!, email: v.email!, documentId: v.documentId!, programId: v.programId! }).subscribe({
        next: (created) => this.router.navigate(['/enrollment', created.id]),
        error: (e) => { this.error.set(apiMessage(e, 'No se pudo guardar.')); this.saving.set(false); }
      });
    }
  }
}
