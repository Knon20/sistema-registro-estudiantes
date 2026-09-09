import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { StudentService } from '../../core/services/student.service';
import { CatalogService } from '../../core/services/catalog.service';
import { ConfirmDialogService } from '../../shared/confirm-dialog/confirm-dialog.service';
import { ProgramDto, DeletedStudentInfo } from '../../core/models';
import { apiMessage } from '../../core/http/api-error';

@Component({
  selector: 'app-student-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <section class="card form-card">
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
          <a [routerLink]="backUrl()" class="btn">Volver</a>
        </div>
      </form>
    </section>
  `,
  styles: [`
    .form-card { max-width: 580px; margin-inline: auto; }
    form { display: grid; gap: 1rem; }
    label { display: grid; gap: .35rem; }
    .row { display: flex; gap: .6rem; flex-wrap: wrap; }
    .row .btn { flex: 1; justify-content: center; min-width: 130px; }
  `]
})
export class StudentFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private api = inject(StudentService);
  private catalog = inject(CatalogService);
  private dialog = inject(ConfirmDialogService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  /** Origin-aware back: list→list, detail→detail; hierarchical fallback. */
  backUrl(): string {
    return this.returnUrl ?? (this.isEdit && this.id ? `/students/${this.id}` : '/students');
  }

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
  private returnUrl: string | null = null;

  ngOnInit(): void {
    this.catalog.programs().subscribe({ next: (res) => this.programs.set(res.items) });
    this.id = this.route.snapshot.paramMap.get('id');
    const requested = this.route.snapshot.queryParamMap.get('returnUrl');
    this.returnUrl = requested && requested.startsWith('/') && !requested.includes('/edit')
      ? requested
      : null;
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
        next: () => this.router.navigateByUrl(this.backUrl()),
        error: (e) => { this.error.set(apiMessage(e, 'No se pudo guardar.')); this.saving.set(false); }
      });
    } else {
      this.create(false);
    }
  }

  private create(forceCreate: boolean): void {
    const v = this.form.getRawValue();
    this.saving.set(true);
    this.error.set(null);
    this.api.create(
      { fullName: v.fullName!, email: v.email!, documentId: v.documentId!, programId: v.programId! },
      forceCreate
    ).subscribe({
      next: (created) => this.router.navigate(['/enrollment', created.id]),
      error: (e) => {
        this.saving.set(false);
        if (!forceCreate && (e.error?.code === 'STUDENT_DELETED_EXISTS') && e.error?.data) {
          this.askReactivate(e.error.data as DeletedStudentInfo);
        } else {
          this.error.set(apiMessage(e, 'No se pudo guardar.'));
        }
      }
    });
  }

  private async askReactivate(candidate: DeletedStudentInfo): Promise<void> {
    const choice = await this.dialog.openThree({
      title: 'El estudiante ya existió',
      message: `${candidate.fullName} (${candidate.email}) tiene un registro eliminado. ¿Reactivarlo o crear uno nuevo con otro ID?`,
      confirmText: 'Reactivar',
      altText: 'Crear nuevo',
      tone: 'brand',
    });
    if (choice === 'confirm') {
      this.saving.set(true);
      this.api.restore(candidate.id).subscribe({
        next: (restored) => this.router.navigate(['/students', restored.id]),
        error: (e) => { this.error.set(apiMessage(e, 'No se pudo reactivar.')); this.saving.set(false); }
      });
    } else if (choice === 'alt') {
      this.create(true);
    }
  }
}
