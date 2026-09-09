# Reglas de negocio

1. **Exactamente 3 materias** por inscripción (`ENROLLMENT_COURSE_COUNT_INVALID` -> 422).
2. **Sin repetir materia** (`COURSE_DUPLICATED` -> 422).
3. **Profesores diferentes**: las 3 materias deben tener `ProfessorId` distintos (`COURSE_PROFESSOR_CONFLICT` -> 422).
4. **9 créditos totales**: cada materia vale 3 (`CREDITS_INVALID` -> 422).
5. **Materias deben existir**: si algún `courseId` no existe -> 404 `COURSE_NOT_FOUND`.
6. **Una inscripción activa por estudiante y periodo**: índice único `(StudentId, Period)`; duplicado -> 409 `ENROLLMENT_DUPLICATED`.
7. **Modificar cumple lo mismo que crear**: `ReplaceCourses` revalida todo.
8. **Email y documento únicos** por estudiante -> 409 `STUDENT_DUPLICATED`.
9. **Privacidad**: compañeros solo expone `{studentId, fullName}`; listado global no expone inscripciones ni documentos ajenos más allá de lo necesario.

Todas las reglas 1–4 viven en `Domain.Entities.EnrollmentRules` + `Enrollment.Create/ReplaceCourses` (no en controllers). Application añade 5–6 (requieren I/O) y el frontend solo las refleja para UX.
