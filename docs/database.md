# Base de datos

## Modelo

```text
Programs (Id PK, Name, Code UNIQUE)
Professors (Id PK, FullName, Email UNIQUE)
Courses (Id PK, Name, Code UNIQUE, Credits, ProfessorId FK->Professors RESTRICT, IX ProfessorId)
Students (Id PK, FullName, Email UNIQUE, DocumentId UNIQUE, ProgramId FK->Programs RESTRICT, CreatedAt)
Enrollments (Id PK, StudentId FK->Students CASCADE, Period, CreatedAt, UpdatedAt, UNIQUE(StudentId, Period))
EnrollmentCourses (EnrollmentId FK->Enrollments CASCADE, CourseId FK->Courses RESTRICT, PK(EnrollmentId, CourseId), IX CourseId)
```

## Integridad

- PKs `uniqueidentifier` (GUID).
- FKs con `ON DELETE CASCADE` solo donde el hijo no tiene sentido sin el padre (inscripción sin estudiante).
- `RESTRICT` en `Courses.ProfessorId` y `EnrollmentCourses.CourseId` para no borrar catálogo con inscripciones.
- Índices únicos: `Programs.Code`, `Professors.Email`, `Courses.Code`, `Students.Email`, `Students.DocumentId`, `Enrollments(StudentId, Period)`.
- Check implícito de créditos: el dominio exige 3 por materia y 9 por inscripción; la columna `Credits` persiste el valor para auditoría.

## Migraciones y scripts

- `src/Infrastructure/Persistence/Migrations/` — migración EF `InitialCreate`.
- `database/init.sql` — script idempotente generado (`dotnet ef migrations script --idempotent`).
- `database/seed.sql` — 2 programas, 5 profesores, 10 materias (2 por profesor), 2 estudiantes demo.

## Seed

Cada profesor dicta exactamente 2 materias:

| Profesor | Materias |
|----------|----------|
| Ana María Torres | MAT-101 Cálculo I, FIS-102 Física I |
| Carlos Ruiz | SIS-103 Programación I, SIS-104 Bases de Datos |
| Lucía Fernández | SIS-105 Redes I, SIS-106 Sistemas Operativos |
| Jorge Ramírez | HUM-107 Inglés Técnico, HUM-108 Ética Profesional |
| Sofía Herrera | MAT-109 Estadística, SIS-110 Algoritmos |
