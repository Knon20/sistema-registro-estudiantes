import { Routes } from '@angular/router';
import { StudentListComponent } from './features/students/student-list.component';
import { StudentFormComponent } from './features/students/student-form.component';
import { StudentDetailComponent } from './features/students/student-detail.component';
import { EnrollmentComponent } from './features/enrollment/enrollment.component';
import { ClassmatesComponent } from './features/consultas/classmates.component';

export const routes: Routes = [
  { path: '', redirectTo: 'students', pathMatch: 'full' },
  { path: 'students', component: StudentListComponent },
  { path: 'students/new', component: StudentFormComponent },
  { path: 'students/:id/edit', component: StudentFormComponent },
  { path: 'students/:id', component: StudentDetailComponent },
  { path: 'enrollment/:studentId', component: EnrollmentComponent },
  { path: 'classmates', component: ClassmatesComponent },
  { path: '**', redirectTo: 'students' },
];
