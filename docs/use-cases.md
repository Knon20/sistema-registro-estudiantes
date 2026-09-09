# Casos de uso

| # | Caso | Entrada | Salida | Errores |
|---|------|---------|--------|---------|
| CU-01 | Crear estudiante | `POST /api/students` `{fullName, email, documentId, programId}` | 201 `StudentDto` | 400 validación, 409 `STUDENT_DUPLICATED` (email o documento) |
| CU-02 | Obtener estudiante | `GET /api/students/{id}` | 200 `StudentDto` | 404 `STUDENT_NOT_FOUND` |
| CU-03 | Listar estudiantes | `GET /api/students?page=1&pageSize=20` | 200 `{items, total}` paginado (SQL SKIP/TAKE) | — |
| CU-04 | Actualizar estudiante | `PUT /api/students/{id}` `{fullName, email, programId}` | 200 `StudentDto` | 404, 409 email en uso |
| CU-05 | Eliminar estudiante | `DELETE /api/students/{id}` | 204 | 404; borra en cascada su inscripción (FK cascade) |
| CU-06 | Catálogo | `GET /api/programs|professors|courses?page=&pageSize=` | 200 `{items, total}` paginado | page<1→1, pageSize clamp 1..100 |
| CU-07 | Crear inscripción | `POST /api/enrollments` `{studentId, period, courseIds[3]}` | 201 `EnrollmentDto` (9 créditos) | 404 student/course, 409 `ENROLLMENT_DUPLICATED`, 422 reglas |
| CU-08 | Consultar inscripción | `GET /api/enrollments/student/{id}?period=` | 200 `EnrollmentDto` / 404 | 404 student o enrollment |
| CU-09 | Actualizar inscripción | `PUT /api/enrollments/student/{id}?period=` `{courseIds[3]}` | 200 `EnrollmentDto` | mismas reglas que CU-07 + 404 si no existe |
| CU-10 | Compañeros por materia | `GET /api/enrollments/courses/{courseId}/classmates?page=&pageSize=` | 200 `{items: [{studentId, fullName}], total}` solo nombres, una sola query batch | 404 si materia no existe; `{items: [], total: 0}` si nadie inscrito |

Periodo por defecto: `2026-1`.
